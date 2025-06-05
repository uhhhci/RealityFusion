/*
 * Copyright (C) 2023, Inria
 * GRAPHDECO research group, https://team.inria.fr/graphdeco
 * All rights reserved.
 *
 * This software is free for non-commercial, research and evaluation use 
 * under the terms of the LICENSE.md file.
 *
 * For inquiries contact Eigen@inria.fr and/or George.Drettakis@inria.fr
 */

#include "GaussianView.h"
#include <thread>
#include <rasterizer.h>
#include <unityRenderKernel.h>

#include <Eigen/Eigen>
#include <fstream>
#include <iostream>
#include <assert.h>

using std::cout;
using std::cerr;
using std::endl;

typedef Eigen::Matrix<float, 3, 1, Eigen::DontAlign> Vector3f;
typedef	Eigen::Matrix<float, 4, 4, Eigen::DontAlign, 4, 4> Matrix4f;

typedef Vector3f Pos;
template<int D>
struct SHs
{
	float shs[(D+1)*(D+1)*3];
};
struct Scale
{
	float scale[3];
};
struct Rot
{
	float rot[4];
};
template<int D>
struct RichPoint
{
	Pos pos;
	float n[3];
	SHs<D> shs;
	float opacity;
	Scale scale;
	Rot rot;
};

float sigmoid(const float m1)
{
	return 1.0f / (1.0f + exp(-m1));
}

float inverse_sigmoid(const float m1)
{
	return log(m1 / (1.0f - m1));
}

# define CUDA_SAFE_CALL_ALWAYS(A) \
A; \
cudaDeviceSynchronize(); \
if (cudaPeekAtLastError() != cudaSuccess) \
std::cout << cudaGetErrorString(cudaGetLastError());

/// Checks the result of a cudaXXXXXX call and throws an error on failure
#define CUDA_CHECK_THROW(x)                                                                                               \
	do {                                                                                                                  \
		cudaError_t result = x;                                                                                           \
		if (result != cudaSuccess)                                                                                        \
			std::cout << cudaGetErrorString(cudaGetLastError()) << std::endl;   \
	} while(0)

#if DEBUG || _DEBUG
# define CUDA_SAFE_CALL(A) CUDA_SAFE_CALL_ALWAYS(A)
#else
# define CUDA_SAFE_CALL(A) A
#endif

// Load the Gaussians from the given file.
template<int D>
int loadPly(const char* filename,
	std::vector<Pos>& pos,
	std::vector<SHs<3>>& shs,
	std::vector<float>& opacities,
	std::vector<Scale>& scales,
	std::vector<Rot>& rot,
	std::vector<int>& selected,
	std::vector<int>& mask,
	Eigen::Vector3f& minn,
	Eigen::Vector3f& maxx)
{
	std::ifstream infile(filename, std::ios_base::binary);

	if (!infile.good())
		std::cout << "Unable to find model's PLY file, attempted:\n" << filename << std::endl;

	// "Parse" header (it has to be a specific format anyway)
	std::string buff;
	std::getline(infile, buff);
	std::getline(infile, buff);

	std::string dummy;
	std::getline(infile, buff);
	std::stringstream ss(buff);
	int count;
	ss >> dummy >> dummy >> count;

	// Output number of Gaussians contained
	std::cout << "Loading " << count << " Gaussian splats" << std::endl;

	while (std::getline(infile, buff))
		if (buff.compare("end_header") == 0)
			break;

	// Read all Gaussians at once (AoS)
	std::vector<RichPoint<D>> points(count);
	infile.read((char*)points.data(), count * sizeof(RichPoint<D>));

	// Resize our SoA data
	pos.resize(count);
	shs.resize(count);
	scales.resize(count);
	rot.resize(count);
	opacities.resize(count);
	selected.resize(count);
	mask.resize(count);
	// Gaussians are done training, they won't move anymore. Arrange
	// them according to 3D Morton order. This means better cache
	// behavior for reading Gaussians that end up in the same tile 
	// (close in 3D --> close in 2D).
	minn = Eigen::Vector3f(FLT_MAX, FLT_MAX, FLT_MAX);
	maxx = -minn;
	for (int i = 0; i < count; i++)
	{
		maxx = maxx.cwiseMax(points[i].pos);
		minn = minn.cwiseMin(points[i].pos);
	}
	std::vector<std::pair<uint64_t, int>> mapp(count);
	for (int i = 0; i < count; i++)
	{
		Eigen::Vector3f rel = (points[i].pos - minn).array() / (maxx - minn).array();
		Eigen::Vector3f scaled = ((float((1 << 21) - 1)) * rel);
		Eigen::Vector3i xyz = scaled.cast<int>();

		uint64_t code = 0;
		for (int i = 0; i < 21; i++) {
			code |= ((uint64_t(xyz.x() & (1 << i))) << (2 * i + 0));
			code |= ((uint64_t(xyz.y() & (1 << i))) << (2 * i + 1));
			code |= ((uint64_t(xyz.z() & (1 << i))) << (2 * i + 2));
		}

		mapp[i].first = code;
		mapp[i].second = i;
	}
	auto sorter = [](const std::pair < uint64_t, int>& a, const std::pair < uint64_t, int>& b) {
		return a.first < b.first;
	};
	std::sort(mapp.begin(), mapp.end(), sorter);

	// Move data from AoS to SoA
	int SH_N = (D + 1) * (D + 1);
	for (int k = 0; k < count; k++)
	{
		int i = mapp[k].second;
		pos[k] = points[i].pos;
		mask[k] = 1;
		selected[k] = 0;
		// Normalize quaternion
		float length2 = 0;
		for (int j = 0; j < 4; j++)
			length2 += points[i].rot.rot[j] * points[i].rot.rot[j];
		float length = sqrt(length2);
		for (int j = 0; j < 4; j++)
			rot[k].rot[j] = points[i].rot.rot[j] / length;

		// Exponentiate scale
		for(int j = 0; j < 3; j++)
			scales[k].scale[j] = exp(points[i].scale.scale[j]);

		// Activate alpha
		opacities[k] = sigmoid(points[i].opacity);

		shs[k].shs[0] = points[i].shs.shs[0];
		shs[k].shs[1] = points[i].shs.shs[1];
		shs[k].shs[2] = points[i].shs.shs[2];
		for (int j = 1; j < SH_N; j++)
		{
			shs[k].shs[j * 3 + 0] = points[i].shs.shs[(j - 1) + 3];
			shs[k].shs[j * 3 + 1] = points[i].shs.shs[(j - 1) + SH_N + 2];
			shs[k].shs[j * 3 + 2] = points[i].shs.shs[(j - 1) + 2 * SH_N + 1];
		}
	}
	return count;
}

void savePly(const char* filename,
	const std::vector<Pos>& pos,
	const std::vector<SHs<3>>& shs,
	const std::vector<float>& opacities,
	const std::vector<Scale>& scales,
	const std::vector<Rot>& rot,
	const Eigen::Vector3f& minn,
	const Eigen::Vector3f& maxx)
{
	// Read all Gaussians at once (AoS)
	int count = 0;
	for (int i = 0; i < pos.size(); i++)
	{
		if (pos[i].x() < minn.x() || pos[i].y() < minn.y() || pos[i].z() < minn.z() ||
			pos[i].x() > maxx.x() || pos[i].y() > maxx.y() || pos[i].z() > maxx.z())
			continue;
		count++;
	}
	std::vector<RichPoint<3>> points(count);

	// Output number of Gaussians contained
	std::cout << "Saving " << count << " Gaussian splats" << std::endl;

	std::ofstream outfile(filename, std::ios_base::binary);

	outfile << "ply\nformat binary_little_endian 1.0\nelement vertex " << count << "\n";

	std::string props1[] = { "x", "y", "z", "nx", "ny", "nz", "f_dc_0", "f_dc_1", "f_dc_2"};
	std::string props2[] = { "opacity", "scale_0", "scale_1", "scale_2", "rot_0", "rot_1", "rot_2", "rot_3" };

	for (auto s : props1)
		outfile << "property float " << s << std::endl;
	for (int i = 0; i < 45; i++)
		outfile << "property float f_rest_" << i << std::endl;
	for (auto s : props2)
		outfile << "property float " << s << std::endl;
	outfile << "end_header" << std::endl;

	count = 0;
	for (int i = 0; i < pos.size(); i++)
	{
		if (pos[i].x() < minn.x() || pos[i].y() < minn.y() || pos[i].z() < minn.z() ||
			pos[i].x() > maxx.x() || pos[i].y() > maxx.y() || pos[i].z() > maxx.z())
			continue;
		points[count].pos = pos[i];
		points[count].rot = rot[i];
		// Exponentiate scale
		for (int j = 0; j < 3; j++)
			points[count].scale.scale[j] = log(scales[i].scale[j]);
		// Activate alpha
		points[count].opacity = inverse_sigmoid(opacities[i]);
		points[count].shs.shs[0] = shs[i].shs[0];
		points[count].shs.shs[1] = shs[i].shs[1];
		points[count].shs.shs[2] = shs[i].shs[2];
		for (int j = 1; j < 16; j++)
		{
			points[count].shs.shs[(j - 1) + 3] = shs[i].shs[j * 3 + 0];
			points[count].shs.shs[(j - 1) + 18] = shs[i].shs[j * 3 + 1];
			points[count].shs.shs[(j - 1) + 33] = shs[i].shs[j * 3 + 2];
		}
		count++;
	}
	outfile.write((char*)points.data(), sizeof(RichPoint<3>) * points.size());
}

std::function<char* (size_t N)> resizeFunctional(void** ptr, size_t& S) {
	auto lambda = [ptr, &S](size_t N) {
		if (N > S)
		{
			if (*ptr)
				CUDA_SAFE_CALL(cudaFree(*ptr));
			CUDA_SAFE_CALL(cudaMalloc(ptr, 2 * N));
			S = 2 * N;
		}
		return reinterpret_cast<char*>(*ptr);
	};
	return lambda;
}

GaussianView::GaussianView(const char* file, bool* messageRead, int sh_degree, bool white_bg, bool useInterop, int device) :
	_dontshow(messageRead),
	_sh_degree(sh_degree)
{
	int num_devices;
	CUDA_SAFE_CALL_ALWAYS(cudaGetDeviceCount(&num_devices));
	_device = device;
	if (device >= num_devices)
	{
		if (num_devices == 0)
			std::cout << "No CUDA devices detected!";
		else
			std::cout << "Provided device index exceeds number of available CUDA devices!";
	}
	CUDA_SAFE_CALL_ALWAYS(cudaSetDevice(device));
	cudaDeviceProp prop;
	CUDA_SAFE_CALL_ALWAYS(cudaGetDeviceProperties(&prop, device));
	if (prop.major < 7)
	{
		std::cout << "Sorry, need at least compute capability 7.0+!";
	}


	// Load the PLY data (AoS) to the GPU (SoA)
	std::vector<Pos> pos;
	std::vector<Rot> rot;
	std::vector<Scale> scale;
	std::vector<float> opacity;
	std::vector<SHs<3>> shs;
	// add buffers to handle different interactions from users in Unity
	std::vector<int> selected;
	std::vector<int> mask;
	if (sh_degree == 1)
	{
		count = loadPly<1>(file, pos, shs, opacity, scale, rot, selected, mask, _scenemin, _scenemax);
	}
	else if (sh_degree == 2)
	{
		count = loadPly<2>(file, pos, shs, opacity, scale, rot, selected, mask, _scenemin, _scenemax);
	}
	else if (sh_degree == 3)
	{
		count = loadPly<3>(file, pos, shs, opacity, scale, rot, selected, mask, _scenemin, _scenemax);
	}

	_boxmin = _scenemin;
	_boxmax = _scenemax;

	int P = count;

	// the following code can not run when PLY is not correctly loaded! Check if PLY file is loaded!!
	// Allocate and fill the GPU data
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&pos_cuda, sizeof(Pos) * P));
	CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(pos_cuda, pos.data(), sizeof(Pos) * P, cudaMemcpyHostToDevice));
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&rot_cuda, sizeof(Rot) * P));
	CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(rot_cuda, rot.data(), sizeof(Rot) * P, cudaMemcpyHostToDevice));
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&shs_cuda, sizeof(SHs<3>) * P));
	CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(shs_cuda, shs.data(), sizeof(SHs<3>) * P, cudaMemcpyHostToDevice));
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&opacity_cuda, sizeof(float) * P));
	CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(opacity_cuda, opacity.data(), sizeof(float) * P, cudaMemcpyHostToDevice));
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&scale_cuda, sizeof(Scale) * P));
	CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(scale_cuda, scale.data(), sizeof(Scale) * P, cudaMemcpyHostToDevice));

	// masking buffers for different interactions with points
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&mask_cuda, sizeof(int) * P));
	CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(mask_cuda, mask.data(), sizeof(int) * P, cudaMemcpyHostToDevice));
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&selected_cuda, sizeof(int) * P));
	CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(selected_cuda, selected.data(), sizeof(int) * P, cudaMemcpyHostToDevice));
	// allocate memories for the spherical interaction parameters 
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&sphere_center_cuda, sizeof(Eigen::Vector3f)));
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&sphere_radius_cuda, sizeof(float)));

	// Create space for view parameters
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&view_cuda, sizeof(Eigen::Matrix4f)));
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&proj_cuda, sizeof(Eigen::Matrix4f)));
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&cam_pos_cuda, 3 * sizeof(float)));
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&background_cuda, 3 * sizeof(float)));
	CUDA_SAFE_CALL_ALWAYS(cudaMalloc((void**)&rect_cuda, 2 * P * sizeof(int)));

	float bg[3] = { white_bg ? 1.f : 0.f, white_bg ? 1.f : 0.f, white_bg ? 1.f : 0.f };
	CUDA_SAFE_CALL(cudaMemcpy(background_cuda, bg, 3 * sizeof(float), cudaMemcpyHostToDevice));
	
	geomBufferFunc = resizeFunctional(&geomPtr, allocdGeom);
	binningBufferFunc = resizeFunctional(&binningPtr, allocdBinning);
	imgBufferFunc = resizeFunctional(&imgPtr, allocdImg);
}

void GaussianView::setUpGraphicsBuffer(int render_w, int render_h, bool useInterop, bool useCPU){
	_render_CPU = useCPU;
	if(_render_CPU){

		glCreateBuffers(1, &imageBuffer);
		glNamedBufferStorage(imageBuffer, render_w * render_h * 3 * sizeof(float), nullptr, GL_DYNAMIC_STORAGE_BIT);
		glCreateBuffers(1, &depthBuffer);
		glNamedBufferStorage(depthBuffer, render_w * render_h * sizeof(float), nullptr, GL_DYNAMIC_STORAGE_BIT);
	
		data = malloc(render_w * render_h * 3 * sizeof(float)); 

		depth_data = malloc(render_w * render_h *  sizeof(float)); 

		if (useInterop)
		{
			if (cudaPeekAtLastError() != cudaSuccess)
			{
				std::cout << "A CUDA error occurred in setup:" << cudaGetErrorString(cudaGetLastError()) << ". Please rerun in Debug to find the exact line!";
			}
			cudaGraphicsGLRegisterBuffer(&imageBufferCuda, imageBuffer, cudaGraphicsRegisterFlagsWriteDiscard);
			cudaGraphicsGLRegisterBuffer(&depthBufferCuda, depthBuffer, cudaGraphicsRegisterFlagsWriteDiscard);

			useInterop &= (cudaGetLastError() == cudaSuccess);
		}
		if (!useInterop)
		{
			//std::cout << "can not register CUDAGL Interop:" << cudaGetErrorString(cudaGetLastError()) << ". Please rerun in Debug to find the exact line!";

			fallback_bytes.resize(render_w * render_h * 3 * sizeof(float));
			cudaMalloc(&fallbackBufferCuda, fallback_bytes.size());

			fallback_bytes_depth.resize(render_w * render_h * sizeof(float));
			cudaMalloc(&fallbackDepthBufferCuda, fallback_bytes_depth.size());

			_interop_failed = true;
		}
	}
	else{
		CUDA_SAFE_CALL_ALWAYS(cudaMalloc(&depth_cuda, render_w * render_h  * sizeof(float)));
		CUDA_SAFE_CALL_ALWAYS(cudaMalloc(&image_cuda, render_w * render_h * 3 * sizeof(float)));
	}

};

void GaussianView::setScalingModifier(float modifier){
	_scalingModifier = modifier;
}


void GaussianView::onPreRenderUnityCPU(){

	image_cuda = nullptr;
	depth_cuda = nullptr;
	if (!_interop_failed)
	{
		// Map OpenGL buffer resource for use with CUDA
		size_t bytes;
		CUDA_SAFE_CALL_ALWAYS(cudaGraphicsMapResources(1, &imageBufferCuda));
		CUDA_SAFE_CALL_ALWAYS(cudaGraphicsResourceGetMappedPointer((void**)&image_cuda, &bytes, imageBufferCuda));

		size_t bytes_d;
		CUDA_SAFE_CALL_ALWAYS(cudaGraphicsMapResources(1, &depthBufferCuda));
		CUDA_SAFE_CALL_ALWAYS(cudaGraphicsResourceGetMappedPointer((void**)&depth_cuda, &bytes_d, depthBufferCuda));
	}
	else
	{
		image_cuda = fallbackBufferCuda;
		depth_cuda = fallbackDepthBufferCuda;
	}
}
void GaussianView::onRenderUnity( float fovy, float aspect, int render_width, int render_height, Eigen::Vector3f pos, Eigen::Matrix4f model, Eigen::Matrix4f view, Eigen::Matrix4f proj)
{
	Eigen::Matrix4f view_mat = view * model;
	Eigen::Matrix4f proj_mat = proj * view * model;	
	// Compute additional view parameters
	float tan_fovy = tan(fovy * 0.5f);
	float tan_fovx = tan_fovy * aspect;

	// Copy frame-dependent data to GPU
	CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(view_cuda, view_mat.data(), sizeof(Eigen::Matrix4f), cudaMemcpyHostToDevice));
	CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(proj_cuda, proj_mat.data(), sizeof(Eigen::Matrix4f), cudaMemcpyHostToDevice));
	CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(cam_pos_cuda, pos.data(), sizeof(float) * 3, cudaMemcpyHostToDevice));

	// Rasterize
	int* rects = _fastCulling ? rect_cuda : nullptr;
	float* boxmin = _cropping ? (float*)&_boxmin : nullptr;
	float* boxmax = _cropping ? (float*)&_boxmax : nullptr;
	CudaRasterizer::Rasterizer::forwardUnity(
		geomBufferFunc,
		binningBufferFunc,
		imgBufferFunc,
		count, _sh_degree, 16,
		background_cuda,
		render_width, 
		render_height,
		pos_cuda,
		shs_cuda,
		nullptr,
		opacity_cuda,
		scale_cuda,
		_scalingModifier,
		rot_cuda,
		nullptr,
		view_cuda,
		proj_cuda,
		cam_pos_cuda,
		tan_fovx,
		tan_fovy,
		false,
		image_cuda,
		depth_cuda,
		mask_cuda,
		selected_cuda,
		nullptr,
		rects,
		boxmin,
		boxmax
	);

	if (cudaPeekAtLastError() != cudaSuccess)
	{
		std::cout << "A CUDA error occurred during rendering:" << cudaGetErrorString(cudaGetLastError()) << ". Please rerun in Debug to find the exact line!";
	}
}

void GaussianView::copyDataToUnityTextureCPU(GLuint dst, GLuint dst_depth, int render_width, int render_height){

	if (!_interop_failed)
	{
		// Unmap OpenGL resource for use with OpenGL
		CUDA_SAFE_CALL_ALWAYS(cudaGraphicsUnmapResources(1, &imageBufferCuda));
		CUDA_SAFE_CALL_ALWAYS(cudaGraphicsUnmapResources(1, &depthBufferCuda));

	}
	else
	{
		CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(fallback_bytes.data(), fallbackBufferCuda, fallback_bytes.size(), cudaMemcpyDeviceToHost));
		glNamedBufferSubData(imageBuffer, 0, fallback_bytes.size(), fallback_bytes.data());

		CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(fallback_bytes_depth.data(), fallbackDepthBufferCuda, fallback_bytes_depth.size(), cudaMemcpyDeviceToHost));
		glNamedBufferSubData(depthBuffer, 0, fallback_bytes_depth.size(), fallback_bytes_depth.data());
	}
	
	// it reduces the framerate by 50% ... lol
	//slow copy to CPU, need to set up a cuda surface and directly splat it via CUDA instead!
	glClear(GL_DEPTH_BUFFER_BIT | GL_COLOR_BUFFER_BIT);
	int size = render_width * render_height * sizeof(float) *3 ;
	glBindBuffer(GL_PIXEL_PACK_BUFFER, imageBuffer); // Bind the PBO	
	glGetBufferSubData(GL_PIXEL_PACK_BUFFER, 0, size, data); 
	glBindTexture(GL_TEXTURE_2D, dst);
	glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, render_width, render_height, GL_RGB,  GL_FLOAT, data);
	glBindTexture(GL_TEXTURE_2D, 0);
	glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);

	// copy depth 
	int size_d = render_width * render_height * sizeof(float) ;
	glBindBuffer(GL_PIXEL_PACK_BUFFER, depthBuffer); // Bind the PBO	
	glGetBufferSubData(GL_PIXEL_PACK_BUFFER, 0, size_d, depth_data); 
	glBindTexture(GL_TEXTURE_2D, dst_depth);
	glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, render_width, render_height, GL_RED,  GL_FLOAT, depth_data);
	glBindTexture(GL_TEXTURE_2D, 0);
	glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);

}

cudaSurfaceObject_t GaussianView::onPreRenderUnityGPU(cudaGraphicsResource_t dst){

	// pre render set up
	cudaArray_t m_mapped_array = {};
	cudaSurfaceObject_t m_surface = {};
	// did not care about handling interop failures... TODO!
	size_t bytes;
	CUDA_SAFE_CALL_ALWAYS(cudaGraphicsMapResources(1, &dst)); 
	CUDA_SAFE_CALL_ALWAYS(cudaGraphicsSubResourceGetMappedArray(&m_mapped_array, dst, 0, 0)); 

	struct cudaResourceDesc resource_desc;
	memset(&resource_desc, 0, sizeof(resource_desc));
	resource_desc.resType = cudaResourceTypeArray;
	resource_desc.res.array.array = m_mapped_array;

	CUDA_SAFE_CALL_ALWAYS(cudaCreateSurfaceObject(&m_surface, &resource_desc));  
	
	if (cudaPeekAtLastError() != cudaSuccess)
	{
		std::cout << "A CUDA error occurred during rendering:" << cudaGetErrorString(cudaGetLastError()) << ". Please rerun in Debug to find the exact line!";
	}
	return m_surface;
};


void GaussianView::copyDataToUnityTextureGPU(cudaGraphicsResource_t dst, cudaSurfaceObject_t m_surface, cudaGraphicsResource_t dst_depth, cudaSurfaceObject_t m_surface_depth , int render_width, int render_height){

	// post rendering copy 
	
	cuda_splat_to_texture(render_width, render_height, image_cuda, depth_cuda, m_surface, m_surface_depth); 
	CUDA_SAFE_CALL_ALWAYS(cudaDeviceSynchronize()); 
	CUDA_SAFE_CALL_ALWAYS(cudaDestroySurfaceObject(m_surface)); 
	CUDA_SAFE_CALL_ALWAYS(cudaGraphicsUnmapResources(1, &dst)); 
	CUDA_SAFE_CALL_ALWAYS(cudaDestroySurfaceObject(m_surface_depth)); 
	CUDA_SAFE_CALL_ALWAYS(cudaGraphicsUnmapResources(1, &dst_depth)); 

	if (cudaPeekAtLastError() != cudaSuccess)
	{
		std::cout << "A CUDA error occurred during rendering:" << cudaGetErrorString(cudaGetLastError()) << ". Please rerun in Debug to find the exact line!";
	}
		
}

void GaussianView::onHideSphereContent(Eigen::Vector3f center, float radius){
	
	CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(sphere_center_cuda, center.data(), sizeof(float) * 3, cudaMemcpyHostToDevice));
	CUDA_SAFE_CALL_ALWAYS(cudaMemcpy(sphere_radius_cuda, &radius, sizeof(float) , cudaMemcpyHostToDevice));
	CudaRasterizer::Rasterizer::hideSphereContent(
		count,
		pos_cuda,
		mask_cuda,
		sphere_center_cuda,
		sphere_radius_cuda
	);

};

GaussianView::~GaussianView()
{
	// Cleanup
	cudaFree(pos_cuda);
	cudaFree(rot_cuda);
	cudaFree(scale_cuda);
	cudaFree(opacity_cuda);
	cudaFree(shs_cuda);
	cudaFree(mask_cuda);
	cudaFree(selected_cuda);
	cudaFree(sphere_center_cuda);
	cudaFree(sphere_radius_cuda);


	cudaFree(view_cuda);
	cudaFree(proj_cuda);
	cudaFree(cam_pos_cuda);
	cudaFree(background_cuda);
	cudaFree(rect_cuda);

	free(data);
	free(depth_data);

	if(_render_CPU){

		if (!_interop_failed)
		{
			cudaGraphicsUnregisterResource(imageBufferCuda);
			cudaGraphicsUnregisterResource(depthBufferCuda);
		}
		else
		{
			cudaFree(fallbackBufferCuda);
			cudaFree(fallbackDepthBufferCuda);
		}
		glDeleteBuffers(1, &imageBuffer);
		glDeleteBuffers(1, &depthBuffer);

	}else{
		cudaFree(image_cuda);
		cudaFree(depth_cuda);
	}

	if (geomPtr)
		cudaFree(geomPtr);
	if (binningPtr)
		cudaFree(binningPtr);
	if (imgPtr)
		cudaFree(imgPtr);

}

