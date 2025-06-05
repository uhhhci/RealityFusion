#include "unityRenderKernel.h"

__global__ void fill_kernel(int width, int height, float value, cudaSurfaceObject_t surface) {
	uint32_t x = threadIdx.x + blockDim.x * blockIdx.x;
	uint32_t y = threadIdx.y + blockDim.y * blockIdx.y;

	if (x < width && y < height) {
		float4 rgba;
		rgba.x = value * x / width * y / height;
		rgba.y = value;
		rgba.z = value * (width - x) / width * (height - y) / height;
		rgba.w = value;
		surf2Dwrite(rgba, surface, (int)sizeof(float4)*x, y, cudaBoundaryModeClamp);
	}
}

__global__ void splat_to_texture_kernel(int width, int height, float* InData, float* depth, cudaSurfaceObject_t surface, cudaSurfaceObject_t surface_depth) {
	uint32_t x = threadIdx.x + blockDim.x * blockIdx.x;
	uint32_t y = threadIdx.y + blockDim.y * blockIdx.y;

	if (x < width && y < height) {

		float4 rgba;
		rgba.x = InData[0 * width * height + (y * width + x)];
		rgba.y = InData[1 * width * height + (y * width + x)];
		rgba.z = InData[2 * width * height + (y * width + x)];
		rgba.w = 1;
		surf2Dwrite(rgba, surface, sizeof(float4) * x, y, cudaBoundaryModeClamp);
		
		float depth_val = depth[y * width + x];
		surf2Dwrite(depth_val, surface_depth, sizeof(float) * x, y, cudaBoundaryModeClamp);
	}
}

template <typename T> T div_round_up(T val, T divisor) {
	return (val + divisor - 1) / divisor;
}

void cuda_fill(int width, int height, float value, cudaSurfaceObject_t surface) {
	const dim3 threads = { 16, 16, 1 };
	const dim3 blocks = { div_round_up<uint32_t>((uint32_t)width, threads.x), div_round_up<uint32_t>((uint32_t)height, threads.y), 1 };
	fill_kernel<<<blocks, threads>>> (width, height, value, surface);
}

void cuda_splat_to_texture(int width, int height, float* rgb, float* depth,  cudaSurfaceObject_t surface, cudaSurfaceObject_t surface_depth) {
	const dim3 threads = { 16, 16, 1 };
	const dim3 blocks = { div_round_up((uint32_t)width, threads.x), div_round_up((uint32_t)height, threads.y), 1 };
	splat_to_texture_kernel<<<blocks, threads>>>(width, height, rgb, depth, surface, surface_depth);
}
