## Version Matching

ZED Box Info:
```console
python ~/Code/utilities/jetsonUtilities/jetsonInfo.py
```

output on ZED Box:
```console
NVIDIA lanai-3636
L4T 32.6.1 [ JetPack 4.6 ]
Ubuntu 18.04.6 LTS
Kernel Version: 4.9.253-tegra
CUDA 10.2.300
CUDA Architecture: NONE
OpenCV version: 4.1.1
OpenCV Cuda: NO
CUDNN: 8.2.1.32
TensorRT: 8.0.1.6
Vision Works: 1.6.0.501
VPI: 1.1.15
Vulcan: 1.2.70

```

[ROS Melodic for support both ZED Cameras and turtlebot3](https://www.stereolabs.com/docs/ros/)


## Installation


1. Install [ROS Meolodic](https://github.com/jetsonhacks/installROS) 

2. set up ```ROS_IP``` and ```ROS_MASTER``` in the .bashrc file
source it.

3. Install turtlebot ros package ``` sudo apt-get install ros-melodic-turtlebot3-* ```

Additional changes to the ZED Box:

1. [Resolve python 2 and 3 conflict in melodic](https://gist.github.com/azidanit/9950aa5408acdbe25f0ec431654da8d6), future python script needs to have a compiler header on top of the file: ```#!/usr/bin/env python```

2. Chnage default python version: ```sudo update-alternatives --install /usr/bin/python python /usr/bin/python2.7 1 ```  and 
```sudo update-alternatives --config python ```

Finally, do a test run:

Launch keyboard teleoperation: ```roslaunch turtlebot3_teleop turtlebot3_teleop_key.launch``` 

 Start gazebo simulation: ```roslaunch turtlebot3_gazebo turtlebot3_world.launch```

This should make the robot run around in the simulation already.

E. To uninstall : ```sudo apt-get remove ros-*```

### OpenCR Kernel on TX2 and Real world operation

1. Open kernel for openCR board: https://github.com/jetsonhacks/jetson-linux-build

2. Give it sudo for the OpenCR micro usb port:  ```sudo chmod 666 /dev/ttyACM0  ```

3. Bring the robot up: ```roslaunch turtlebot3_bringup turtlebot3_robot.launch``` 

4. Launch keyboard teleoperation: ```roslaunch turtlebot3_teleop turtlebot3_teleop_key.launch``` 


Note> Make sure that you connect the OpenCR board to a power source with correct (12V) output and switch on the board, otherwise it will only read data from the micro-usb port but not actually driving the motors.

## ZED Camera Setup 

We are using ZED version 3.7.1 ---> make sure this is the case for ZED SDK in all computers.

### ZED Local streaming script

1. The default streaming script that could be directly connected to Unity can be found in the following path: 
```/usr/local/zed/samples/"camera streaming"/sender/python```

2. Switch to the correct python for running the script: 

```console 

sudo update-alternatives --config python

  Selection    Path                Priority   Status
------------------------------------------------------------
  0            /usr/bin/python3.6   2         auto mode
  1            /usr/bin/python2.7   1         manual mode
  2            /usr/bin/python3.6   2         manual mode
* 3            /usr/bin/python3.7   1         manual mode

```

3. Choose 2

### ZED ROS Wrapper

1. Since the ZED Box has Ubuntu 18.04, we need to pull the ZED Ros wrapper for this [SDK tag](https://github.com/stereolabs/zed-ros-wrapper/tree/v3.7.x)

```console
 git clone --recursive  https://github.com/stereolabs/zed-ros-wrapper.git  --branch v3.7.x --single-branch
```

2. Open a new console and do ```cd ~/catkin_ws && source ./devel/setup.bash```

3. roslaunch for the zedmini camera: ```roslaunch zed_wrapper zedm.launch```


## Basic Robot bring up:


1. Run ```roscore```

2. Give it sudo for the OpenCR micro usb port:  ```sudo chmod 666 /dev/ttyACM0  ```

3. Bring the robot up: ```roslaunch turtlebot3_bringup turtlebot3_robot.launch``` 

4. Launch keyboard teleoperation: ```roslaunch turtlebot3_teleop turtlebot3_teleop_key.launch``` 

Result: 

  <img src="../images/01_first_bringup_turtlebot3.gif"
          alt="turtlebot3 first bring up"
          style="float: center;  height:340px" />

## Robot Telelop from a remote PC, config:

On the ZED Box: ```ROS_MASRER_URI=http://[REMOTE]:11311 ``` && ```roslaunch turtlebot3_bringup turtlebot3_robot.launch```
On the Remote PC: ```roscore && roslaunch turtlebot3_teleop turtlebot3_teleop_key.launch```