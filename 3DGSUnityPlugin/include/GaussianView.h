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
#pragma once
#include <windows.h>

#include <PlatformBase.h>
#if SUPPORT_OPENGL_UNIFIED
#if UNITY_IOS || UNITY_TVOS
#	include <OpenGLES/ES2/gl.h>
#elif UNITY_ANDROID || UNITY_WEBGL
#	include <GLES2/gl2.h>
#elif UNITY_OSX
#	include <OpenGL/gl3.h>
#elif UNITY_WIN
// On Windows, use gl3w to initialize and load OpenGL Core functions. In principle any other
// library (like GLEW, GLFW etc.) can be used; here we use gl3w since it's simple and
// straightforward.
#include <GL/glew.h>	
#elif UNITY_LINUX
#	define GL_GLEXT_PROTOTYPES
#	include <GL/gl.h>
#elif UNITY_EMBEDDED_LINUX
#	include <GLES2/gl2.h>
#else
#	error Unknown platform
#endif

#if SUPPORT_OPENGL_CORE
#	define GL_GLEXT_PROTOTYPES
#	include <GL/gl.h>
#endif
#endif

#include <memory>
#include <cuda_runtime.h>
#include <cuda_gl_interop.h>
#include <functional>
#include "unityRenderKernel.h"

#include <Eigen/Eigen>
typedef Eigen::Matrix<float, 3, 1, Eigen::DontAlign> Vector3f;
typedef	Eigen::Matrix<float, 4, 4, Eigen::DontAlign, 4, 4> Matrix4f;

namespace CudaRasterizer
{
	class Rasterizer;
}

class GaussianView 
{

public:

	GaussianView(const char* file, bool* message_read, int sh_degree, bool white_bg = false, bool useInterop = true, int device = 0);

	void onRenderUnity(float fovy, float aspect, int render_width, int render_height, Eigen::Vector3f pos, Eigen::Matrix4f model, Eigen::Matrix4f view, Eigen::Matrix4f proj);

	void setUpGraphicsBuffer(int render_w, int render_h, bool useInterop, bool useCPU);

	void onPreRenderUnityCPU();

	void copyDataToUnityTextureCPU(GLuint dst, GLuint dst_depth, int render_width, int render_height);

	cudaSurfaceObject_t onPreRenderUnityGPU(cudaGraphicsResource_t dst);

	void copyDataToUnityTextureGPU(cudaGraphicsResource_t dst, cudaSurfaceObject_t m_surface, cudaGraphicsResource_t dst_depth, cudaSurfaceObject_t m_surface_depth ,int render_width, int render_height);

	void onHideSphereContent(Eigen::Vector3f center, float radius);

	void setScalingModifier(float modifier);

	virtual ~GaussianView();

	bool* _dontshow;

protected:

	bool _render_CPU = false;
	bool _cropping = false;

	Eigen::Vector3f _boxmin, _boxmax, _scenemin, _scenemax;
	char _buff[512] = "cropped.ply";

	bool _fastCulling = true;
	int _device = 0;
	int _sh_degree = 3;

	int count;
	float* pos_cuda;
	float* rot_cuda;
	float* scale_cuda;
	float* opacity_cuda;
	float* shs_cuda;
	int* rect_cuda;
	// interaction with unity
	int* mask_cuda;
	int* selected_cuda;
	float* sphere_center_cuda;
	float* sphere_radius_cuda;

	GLvoid *data;
	GLvoid *depth_data;
	float* image_cuda;
	float* depth_cuda;

	GLuint imageBuffer;
	GLuint depthBuffer;
	cudaGraphicsResource_t imageBufferCuda;
	cudaGraphicsResource_t depthBufferCuda;

	size_t allocdGeom = 0, allocdBinning = 0, allocdImg = 0;
	void* geomPtr = nullptr, * binningPtr = nullptr, * imgPtr = nullptr;
	std::function<char* (size_t N)> geomBufferFunc, binningBufferFunc, imgBufferFunc;

	float* view_cuda;
	float* proj_cuda;
	float* cam_pos_cuda;
	float* background_cuda;

	float _scalingModifier = 1.0f;

	bool _interop_failed = false;
	std::vector<char> fallback_bytes;
	float* fallbackBufferCuda = nullptr;
	std::vector<char> fallback_bytes_depth;
	float* fallbackDepthBufferCuda = nullptr;

	bool accepted = false;

};


