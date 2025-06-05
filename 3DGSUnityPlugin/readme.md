## Compile Main Unity Gaussian Viewer

cmake -G "Visual Studio 16" . -B build
cmake --build build --config Release -j 4

Then copy the ``unity_gaussian.dll`` file into the ``Assets\Plugins\x86_64`` folder
