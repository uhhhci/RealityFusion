#include "UnityRenderer.h"
#include <PlatformBase.h>
#include <assert.h>
#include <cuda_runtime.h>
#include <cuda_gl_interop.h>
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

#include "GaussianView.h"
#include "Unity/IUnityInterface.h"
#include "Unity/IUnityGraphics.h"
#include <algorithm>
#include <regex>


#include <fstream>
#include <iostream>
#include <assert.h>

using std::cout;
using std::cerr;
using std::endl;

// event flags
const int INIT_EVENT       = 0x0001;
const int DEINIT_EVENT     = 0x0003;
const int CREATE_TEX       = 0x0004;
const int DRAW_EVENT_LEFT  = 0x0005;
const int DRAW_EVENT_RIGHT = 0x0006;

// flags
bool graphics_initialized = false;
bool use_depth = false;
bool use_cpu   = false; // very slow, should always set to false unless necessary

//gaussian related stuffs
std::string plyfile;
int sh_degree;
bool white_background;
int device=0;

std::shared_ptr<GaussianView> gaussianView;

int rendering_width;
int rendering_height;
GLuint leftHandle  = 0;
GLuint rightHandle = 0;
GLuint leftHandleDepth  = 0;
GLuint rightHandleDepth = 0;
float aspect = 1;
float fovy = 3.14;
cudaGraphicsResource_t imageBufferCudaLeft = nullptr;
cudaGraphicsResource_t imageBufferCudaRight = nullptr;
cudaGraphicsResource_t depthBufferCudaLeft = nullptr;
cudaGraphicsResource_t depthBufferCudaRight = nullptr;

Eigen::Vector3f leftEyePos = Eigen::Vector3f(0, 0, 0);
Eigen::Vector3f rightEyePos = Eigen::Vector3f(0, 0, 0);
Eigen::Matrix4f gsModelMatrix = Matrix4f::Identity();
Eigen::Matrix4f gsLeftViewMat = Matrix4f::Identity();
Eigen::Matrix4f gsRightViewMat = Matrix4f::Identity();
Eigen::Matrix4f gsProjMatLeft  = Matrix4f::Identity();
Eigen::Matrix4f gsProjMatRight = Matrix4f::Identity();


// TODO: make the code more organized with a struct for each eye!
struct GSEye{

	Eigen::Vector3f pos;
	Eigen::Matrix4f viewMat;
	Eigen::Matrix4f projMat;
	GLuint texHandle;
	GLuint depthHandle;
};
std::shared_ptr<GSEye> leftEye;
std::shared_ptr<GSEye> rightEye;


std::pair<int, int> findArg(const std::string& line, const std::string& name)
{
	int start = line.find(name, 0);
	start = line.find("=", start);
	start += 1;
	int end = line.find_first_of(",)", start);
	return std::make_pair(start, end);
}

void CheckOpenGLVersion()
{
    const GLubyte *version = glGetString(GL_VERSION);
    if (version)
    {
		std::cout<<"OpenGL Version: "<< version<< std::endl;
    }
    else
    {
        std::cout<<"Unable to retrieve OpenGL version.\n"<< std::endl;
    }
}

static UnityGfxRenderer s_DeviceType = UnityGfxRenderer::kUnityGfxRendererNull;
static IUnityInterfaces* s_UnityInterfaces = NULL;
static IUnityGraphics* s_Graphics = NULL;

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);
extern "C" void	UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
	s_UnityInterfaces = unityInterfaces;
	s_Graphics = s_UnityInterfaces->Get<IUnityGraphics>();
	s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
	
	// Run OnGraphicsDeviceEvent(initialize) manually on plugin load
	OnGraphicsDeviceEvent(UnityGfxDeviceEventType::kUnityGfxDeviceEventInitialize);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
	s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
}

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
	// Create graphics API implementation upon initialization
	if (eventType == UnityGfxDeviceEventType::kUnityGfxDeviceEventInitialize)
	{
        // again, we are assuming it is OpenGLCore
		s_DeviceType = s_Graphics->GetRenderer();
        if(s_DeviceType != UnityGfxRenderer::kUnityGfxRendererOpenGLCore){
            std::cout << "unsupported graphics device type";
            return;
		}
        // else{
		// 	if (!glfwInit()) {
		// 	std::cout << "Could not initialize glfw" << std::endl;}
		// 	std::cout << "GLFW Initialized" << std::endl;

        // }
	}
	if (eventType == UnityGfxDeviceEventType::kUnityGfxDeviceEventShutdown)
	{    

		s_DeviceType = UnityGfxRenderer::kUnityGfxRendererNull;
	}
}



extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_test_func() {

    std::cout<<"Hello from unity 3D Gaussian plugin "<<endl;

}


extern "C" bool	UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_get_initialization_state(){
	
	return graphics_initialized;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_set_init_values(const char* model_path, bool useDepth, bool useCPU) {
	graphics_initialized = false;
    use_depth = useDepth;
	use_cpu   = useCPU;
	
	device = 0;
	sh_degree = 3;
	white_background = false;

	plyfile = model_path;
    std::cout<<"Initialized Gaussian plugin "<<endl;

}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_initialize_graphics(){
    if(graphics_initialized){
       std::cout << "graphics already initialized" ;
        return;
    }

	// if (!glfwInit()) {
	// 	std::cout << "Could not initialize glfw" << std::endl;
	// }

	glewExperimental = GL_TRUE;
	GLenum err = glewInit();
#ifdef GLEW_EGL
	if (err != GLEW_OK && (!args.offscreen || err != GLEW_ERROR_NO_GLX_DISPLAY)) // Small hack for glew, this error occurs but does not concern offscreen
	if (err != GLEW_OK && (!args.offscreen )) // Small hack for glew, this error occurs but does not concern offscreen
#else
	if (err != GLEW_OK)
#endif
	std::cout << "cannot initialize GLEW (used to load OpenGL function)" << std::endl;
	(void)glGetError(); // I notice that glew might do wrong things during its init()
							// some drivers complain about it. So I reset OpenGL's errors to discard this.

	CheckOpenGLVersion();

	bool messageRead = false;
	// need two views because each view contains its' own image buffer, so we could process it in parrallel
	gaussianView.reset(new GaussianView(plyfile.c_str(), &messageRead, sh_degree, white_background, true, device));

	std::cout << "Done initializing Gaussian view" << std::endl;

	graphics_initialized = true;
};



extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_create_texture(){
	
	gaussianView->setUpGraphicsBuffer(rendering_width, rendering_height, true, use_cpu);

	//float aspect = rendering_width / rendering_height;
	// create texture for left eye
	glGenTextures(1, &leftHandle);
	glBindTexture(GL_TEXTURE_2D, leftHandle);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32F, rendering_width, rendering_height, 0, GL_RGBA, GL_FLOAT, 0);
	//glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, rendering_width, rendering_height, 0, GL_RGB, GL_FLOAT, 0);
	if(!use_cpu){
		cudaGraphicsGLRegisterImage(&imageBufferCudaLeft, leftHandle, GL_TEXTURE_2D, cudaGraphicsRegisterFlagsSurfaceLoadStore);
	}
	glBindTexture(GL_TEXTURE_2D, 0);

	//先不管你了。。 一會再回來看。。buffer 也都還沒建， 看看其他的能不能跑先
	// create depth texture for left eye
	glGenTextures(1, &leftHandleDepth);
	glBindTexture(GL_TEXTURE_2D, leftHandleDepth);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
	glTexImage2D(GL_TEXTURE_2D, 0, GL_R32F, rendering_width, rendering_height, 0, GL_RED, GL_FLOAT, 0);
	if(!use_cpu){
		cudaGraphicsGLRegisterImage(&depthBufferCudaLeft, leftHandleDepth, GL_TEXTURE_2D, cudaGraphicsRegisterFlagsSurfaceLoadStore);
	}
	glBindTexture(GL_TEXTURE_2D, 0);

	// create texture for right eye 
	glGenTextures(1, &rightHandle);
	glBindTexture(GL_TEXTURE_2D, rightHandle);
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32F, rendering_width, rendering_height, 0, GL_RGBA, GL_FLOAT, 0);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
	if(!use_cpu){
		cudaGraphicsGLRegisterImage(&imageBufferCudaRight, rightHandle, GL_TEXTURE_2D, cudaGraphicsRegisterFlagsSurfaceLoadStore);	
	}
	glBindTexture(GL_TEXTURE_2D, 0);

	// create depth texture for right eye
	glGenTextures(1, &rightHandleDepth);
	glBindTexture(GL_TEXTURE_2D, rightHandleDepth);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
	glTexImage2D(GL_TEXTURE_2D, 0, GL_R32F, rendering_width, rendering_height, 0, GL_RED, GL_FLOAT, 0);
	if(!use_cpu){
		cudaGraphicsGLRegisterImage(&depthBufferCudaRight, rightHandleDepth, GL_TEXTURE_2D, cudaGraphicsRegisterFlagsSurfaceLoadStore);
	}
	glBindTexture(GL_TEXTURE_2D, 0);

	std::cout << "Texture Setuped" << std::endl;

};


extern "C" GLuint UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_get_left_handle(){
	return leftHandle;
};

extern "C" GLuint UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_get_right_handle(){
	return rightHandle;
};

extern "C" GLuint UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_get_left_depth_handle(){
	return leftHandleDepth;
};

extern "C" GLuint UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_get_right_depth_handle(){
	return rightHandleDepth;
};

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_set_scaling_modifier(float modifier){
	// TODO: make sure that value is between 0,001 and 1
	gaussianView->setScalingModifier(modifier);

};

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_set_camera_aspect(float a){
	aspect = a;
};


extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_set_camera_fov(float fov){
	fovy = fov;
};

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_texture_left(){
    if(use_cpu)
	{
		gaussianView->onPreRenderUnityCPU();
		gaussianView->onRenderUnity(fovy, aspect,  rendering_width, rendering_height, leftEyePos, gsModelMatrix, gsLeftViewMat, gsProjMatLeft);
		gaussianView->copyDataToUnityTextureCPU(leftHandle, leftHandleDepth, rendering_width, rendering_height);
	}
	else
	{
		cudaSurfaceObject_t m_surface =  gaussianView->onPreRenderUnityGPU(imageBufferCudaLeft);
		cudaSurfaceObject_t m_surface_depth =  gaussianView->onPreRenderUnityGPU(depthBufferCudaLeft);
		gaussianView->onRenderUnity(fovy, aspect,  rendering_width, rendering_height, leftEyePos, gsModelMatrix, gsLeftViewMat, gsProjMatLeft);
		gaussianView->copyDataToUnityTextureGPU(imageBufferCudaLeft, m_surface, depthBufferCudaLeft, m_surface_depth, rendering_width, rendering_height);
	}	

};

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_texture_right(){
	if(use_cpu)
	{
		gaussianView->onPreRenderUnityCPU();
		gaussianView->onRenderUnity(fovy, aspect,  rendering_width, rendering_height, rightEyePos, gsModelMatrix, gsRightViewMat, gsProjMatRight);
		gaussianView->copyDataToUnityTextureCPU(rightHandle, rightHandleDepth, rendering_width, rendering_height);
	}
	else
	{
		cudaSurfaceObject_t m_surface = gaussianView->onPreRenderUnityGPU(imageBufferCudaRight);
		cudaSurfaceObject_t m_surface_depth =  gaussianView->onPreRenderUnityGPU(depthBufferCudaRight);
		gaussianView->onRenderUnity(fovy, aspect,  rendering_width, rendering_height, rightEyePos, gsModelMatrix, gsRightViewMat, gsProjMatRight);
		gaussianView->copyDataToUnityTextureGPU(imageBufferCudaRight, m_surface, depthBufferCudaRight, m_surface_depth, rendering_width, rendering_height);
	}

};


extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_render_resolution(int width, int height){
	rendering_width = width;
	rendering_height = height; 
};


extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_leftview_matrix(float view[]){
	gsLeftViewMat<< view[0], view[1], view[2], view[3],
					view[4], view[5], view[6], view[7],
					view[8], view[9], view[10], view[11],
					view[12], view[13], view[14], view[15];
};

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_rightview_matrix(float view[]){
	gsRightViewMat<< view[0], view[1], view[2], view[3],
					 view[4], view[5], view[6], view[7],
					 view[8], view[9], view[10], view[11],
					 view[12], view[13], view[14], view[15];
};

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_proj_matrix_left(float proj[]){
	gsProjMatLeft<< proj[0], proj[1], proj[2], proj[3],
					proj[4], proj[5], proj[6], proj[7],
					proj[8], proj[9], proj[10], proj[11],
					proj[12], proj[13], proj[14], proj[15];
};

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_proj_matrix_right(float proj[]){
	gsProjMatRight<< proj[0], proj[1], proj[2], proj[3],
					proj[4], proj[5], proj[6], proj[7],
					proj[8], proj[9], proj[10], proj[11],
					proj[12], proj[13], proj[14], proj[15];
};
// these does not work! Try to clean up  the code later
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_transform_left_camera(float pos[]){
	Vector3f leftEyePos(pos[0], pos[1], pos[2]);

};

// these does not work! Try to clean up  the code later
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_transform_right_camera(float pos[]){
	Vector3f rightEyePos(pos[0], pos[1], pos[2]);
};

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_model_matrix(float model[]){
	
		gsModelMatrix << model[0], model[1],   model[2],  model[3],
						 model[4], model[5],   model[6],  model[7],
						 model[8], model[9],   model[10], model[11],
						 model[12], model[13], model[14], model[15];
};


extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_deinitialize_graphics(){

	
	//glfwTerminate();

	if (imageBufferCudaLeft) { cudaGraphicsUnregisterResource(imageBufferCudaLeft); imageBufferCudaLeft = nullptr; }
	if (imageBufferCudaRight) { cudaGraphicsUnregisterResource(imageBufferCudaRight); imageBufferCudaRight = nullptr; }
	if (depthBufferCudaLeft) { cudaGraphicsUnregisterResource(depthBufferCudaLeft); depthBufferCudaLeft = nullptr; }
	if (depthBufferCudaRight) { cudaGraphicsUnregisterResource(depthBufferCudaRight); depthBufferCudaRight = nullptr; }
	
	glBindTexture(GL_TEXTURE_2D, 0);
    glDeleteTextures(1, &leftHandle);
    glDeleteTextures(1, &rightHandle);
    glDeleteTextures(1, &leftHandleDepth);
    glDeleteTextures(1, &rightHandleDepth);

	leftHandle  = 0;
	rightHandle = 0;
	leftHandleDepth  = 0;
	rightHandleDepth = 0;

	gaussianView.reset();
	graphics_initialized = false;
	std::cout<< "graphics deinitialized" << std::endl; 
	
}

static void UNITY_INTERFACE_API unity_gaussian_run_on_render_thread(int eventID)
{

    switch (eventID)
    {

        case INIT_EVENT:
            
            unity_gaussian_initialize_graphics();

            break;

        case CREATE_TEX:
			
			unity_gaussian_create_texture();
            
			break;

        case DRAW_EVENT_LEFT:   
			
			unity_gaussian_update_texture_left();
            
			break;

        case DRAW_EVENT_RIGHT:   
			
			unity_gaussian_update_texture_right();

            break;

        case DEINIT_EVENT:

			unity_gaussian_deinitialize_graphics();
            break;

    }
}

extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunc(){

	return unity_gaussian_run_on_render_thread;
}


// Interaction code!

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_hide_sphere_content(float center[], float radius){
	gaussianView->onHideSphereContent(Vector3f(center[0], center[1], center[2]), radius);
};


