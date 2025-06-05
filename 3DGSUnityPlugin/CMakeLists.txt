cmake_minimum_required(VERSION 3.18)

project(unity_gaussian
	VERSION 1.0
	DESCRIPTION "Gaussian Splatting for unity (K.Li's extension)"
	LANGUAGES C CXX CUDA
)


file(GLOB SOURCES "*.cpp" "*.h" "*.hpp")
source_group("Source Files" FILES ${SOURCES})

set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} ${CMAKE_CURRENT_SOURCE_DIR}/cmake)

###############################################################################
# Build type and C++ compiler setup
###############################################################################

# Set a default configuration if none was specified
if (NOT CMAKE_BUILD_TYPE AND NOT CMAKE_CONFIGURATION_TYPES)
	message(STATUS "No release type specified. Setting to 'Release'.")
	set(CMAKE_BUILD_TYPE Release CACHE STRING "Choose the type of build." FORCE)
	set_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS "Debug" "Release" "RelWithDebInfo")
endif()

if (MSVC)
	set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /D_CRT_SECURE_NO_WARNINGS")
	set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /MP24")
else()
	set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC")
endif()

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_EXTENSIONS OFF)

###############################################################################
# CUDA compiler setup
###############################################################################

set(CMAKE_CUDA_STANDARD 14)
set(CMAKE_CUDA_STANDARD_REQUIRED ON)
set(CMAKE_CUDA_EXTENSIONS OFF)
set(CUDA_LINK_LIBRARIES_KEYWORD PUBLIC)
set(CMAKE_CUDA_RUNTIME_LIBRARY Shared)

if (MSVC)
	list(APPEND CUDA_NVCC_FLAGS "-Xcompiler=/bigobj")
else()
	list(APPEND CUDA_NVCC_FLAGS "-Xcompiler=-Wno-float-conversion")
	list(APPEND CUDA_NVCC_FLAGS "-Xcompiler=-fno-strict-aliasing")
	list(APPEND CUDA_NVCC_FLAGS "-Xcompiler=-fPIC")
endif()
list(APPEND CUDA_NVCC_FLAGS "--extended-lambda")
list(APPEND CUDA_NVCC_FLAGS "--expt-relaxed-constexpr")
list(APPEND CUDA_NVCC_FLAGS "--use_fast_math")

###############################################################################
# Dependencies
###############################################################################

find_package(OpenGL REQUIRED)

if (MSVC)
  list(APPEND DEPENDENCIES_INCLUDE_DIR "dependencies/gl3w")
  list(APPEND UNITY_SOURCES "dependencies/gl3w/GL/gl3w.c")
  list(APPEND UNITY_LIBRARIES opengl32)
  
  list(APPEND DEPENDENCIES_INCLUDE_DIR "dependencies/glew/glew-2.0.0/include")
  list(APPEND LINK_DIR "dependencies/glew/glew-2.0.0/lib64")
  list(APPEND UNITY_LIBRARIES glew32 glew32s glew32d glew32sd)
else()
  find_package(GLEW REQUIRED)
  list(APPEND DEPENDENCIES_INCLUDE_DIR ${GLEW_INCLUDE_DIRS})
endif()

add_subdirectory("dependencies/CudaRasterizer")
list(APPEND UNITY_LIBRARIES CudaRasterizer)

list(APPEND DEPENDENCIES_INCLUDE_DIR "dependencies/eigen/include")
list(APPEND DEPENDENCIES_INCLUDE_DIR "dependencies/eigen/include/Eigen")

###############################################################################
# Library
###############################################################################

list(APPEND UNITYNI_DEFINITIONS -DUNITYNI_VERSION="${UNITYNI_VERSION}")
list(APPEND DEPENDENCIES_INCLUDE_DIR "include")

# Add unity headers
file (GLOB UNITY_HEADERS "include/Unity/*.h")
list(APPEND UNITY_HEADERS ${UNITY_HEADERS})

if (NOT MSVC)
	list(APPEND UNITY_LIBRARIES ${CMAKE_DL_LIBS})
endif()

list(APPEND UNITY_HEADERS
)

list(APPEND UNITY_SOURCES
  ${UNITY_HEADERS}
  include/PlatformBase.h
  include/GaussianView.h
  include/UnityRenderer.h
  
  source/UnityRenderer.cpp
  source/GaussianView.cpp
)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR})
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE ${CMAKE_BINARY_DIR})
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_RELWITHDEBINFO ${CMAKE_BINARY_DIR})
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_MINSIZEREL ${CMAKE_BINARY_DIR})
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_DEBUG ${CMAKE_BINARY_DIR})

get_filename_component(CUDA_COMPILER_BIN "${CMAKE_CUDA_COMPILER}" DIRECTORY)
get_filename_component(CUDA_DIR "${CUDA_COMPILER_BIN}" DIRECTORY)
set(CUDA_INCLUDE "${CUDA_DIR}/include")
set(PROJECT_NAME unity_gaussian)

add_library(${PROJECT_NAME} SHARED ${UNITY_SOURCES})
set_target_properties(${PROJECT_NAME} PROPERTIES CUDA_RESOLVE_DEVICE_SYMBOLS ON CUDA_SEPARABLE_COMPILATION ON)
target_compile_definitions(${PROJECT_NAME} PUBLIC ${UNITYNI_DEFINITIONS})
target_compile_options(${PROJECT_NAME} PUBLIC $<$<COMPILE_LANGUAGE:CUDA>:${CUDA_NVCC_FLAGS}>)
target_include_directories(${PROJECT_NAME} PUBLIC ${DEPENDENCIES_INCLUDE_DIR})
target_link_directories(${PROJECT_NAME} PUBLIC ${LINK_DIR})
target_link_libraries(${PROJECT_NAME} PUBLIC ${UNITY_LIBRARIES})

# Copy shared libraries to the binary directory as needed
if (MSVC)
	file(GLOB CUDA_DLLS "${CUDA_COMPILER_BIN}/cudart64*.dll")
	if (CUDA_DLLS)
		add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD COMMAND ${CMAKE_COMMAND} -E copy ${CUDA_DLLS} "${CMAKE_CURRENT_BINARY_DIR}" COMMAND_EXPAND_LISTS)
	endif()
endif()
