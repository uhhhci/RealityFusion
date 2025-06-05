## Reality Fusion

**Reality fusion** is a high performance and robust immersive robot teleoperation system that combines the best of both worlds: the high fidelity of neural rendering (3D Gaussian Splattings) and real-time stereoscopic point cloud projection. 

  <img src="./images/realityFusionDemo.gif"
      alt="reality fusion" 
      style="height:350px;"/>


## Documentations

1. [Reality Fusion Unity Project](./docs/unity.md): Documentations for the ```RealityFusionUnity``` Unity project and source code for VR robot control applciations. 
2. [Native Render Plugin](./docs/renderplugin.md): Instruction for compiling the original 3DGS Cmake project for a Unity native render plugin. Apre-compiled DLL files already avaliable in ```Assest\Plugins\x86x64``` in the Unity project. The source code for the native renderer is in the ``3DGSUnityPlugin`` folder. 
3. [Reality Fusion Robot Setup](./docs/robot.md): Documentation for setting up the robot, including required packages. 

## Citation

Link to paper: [Reality Fusion: Robust Real-time Immersive Mobile Robot Teleoperation with Volumetric Visual Data Fusion](https://www.edit.fis.uni-hamburg.de/ws/files/55138101/Camera_Ready.pdf)

```bibtex
@INPROCEEDINGS{10802431,
  author={Li, Ke and Bacher, Reinhard and Schmidt, Susanne and Leemans, Wim and Steinicke, Frank},
  booktitle={2024 IEEE/RSJ International Conference on Intelligent Robots and Systems (IROS)}, 
  title={Reality Fusion: Robust Real-time Immersive Mobile Robot Teleoperation with Volumetric Visual Data Fusion}, 
  year={2024},
  volume={},
  number={},
  pages={8982-8989},
  keywords={Visualization;Three-dimensional displays;Telepresence;Robot control;Virtual reality;Robot sensing systems;Rendering (computer graphics);Spatial resolution;Streams;Research and development},
  doi={10.1109/IROS58592.2024.10802431}}

```
Contact: keli95566@gmail.com

## Acknowledgment

This work was supported by DASHH (Data Science in Hamburg - HELMHOLTZ Graduate School for the Structure of Matter) with the Grant-No. HIDSS-0002.

## License

Please check out [INRIA's original license](./LICENSE.md) regarding the original implementation of 3DGS. 