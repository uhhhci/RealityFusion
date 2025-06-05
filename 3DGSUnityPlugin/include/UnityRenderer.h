#pragma once
#include <windows.h>
#include <assert.h>

#include "PlatformBase.h"

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
#include "GL/glew.h"

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
#endif // SUPPORT_OPENGL_UNIFIED

#ifdef _MSC_VER
    #define INTERFACE_API __stdcall
    #define EXPORT_API __declspec(dllexport)
#else
    #define EXPORT_API
    #error "Unsported compiler have fun"
#endif
#include "Unity/IUnityInterface.h"
#include "Unity/IUnityGraphics.h"
// Certain Unity APIs (GL.IssuePluginEvent, CommandBuffer.IssuePluginEvent) can callback into native plugins.
// Provide them with an address to a function of this signature.
typedef void (INTERFACE_API* UnityRenderingEvent)(int eventId);
typedef void (INTERFACE_API* UnityRenderingEventAndData)(int eventId, void* data);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_test_func();

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_transform_left_camera(float pos[]);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_transform_right_camera(float pos[]);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_leftview_matrix(float view[]);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_rightview_matrix(float view[]);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_proj_matrix_left(float proj[]);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_proj_matrix_right(float proj[]);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_model_matrix(float model[]);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_update_render_resolution(int width, int height);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_set_camera_fov(float fov);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_set_camera_aspect(float aspect);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_set_scaling_modifier(float modifier);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_set_init_values(const char* model_path, bool useDepth, bool useCPU);

extern "C" GLuint UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_get_left_handle();

extern "C" GLuint UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_get_right_handle();

extern "C" GLuint UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_get_left_depth_handle();

extern "C" GLuint UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_get_right_depth_handle();

extern "C" bool	UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_get_initialization_state();

extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunc();

extern "C" void	UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces);

// functions for interaction

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unity_gaussian_hide_sphere_content(float center[], float radius);
