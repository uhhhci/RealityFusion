## Robot Startup Guide

1. Activate OpenCR micro usb port and start the turtlebot in ROS: ```sudo chmod 666 /dev/ttyACM0 && roslaunch turtlebot3_bringup turtlebot3_robot.launch``` 
2. Start ```ros_tcp_endpoint``` for establishing communication with Unity via local network: ```rosrun ros_tcp_endpoint default_server_endpoint.py```
3. Switch to ZED camera python streaming path: ```cd /usr/local/zed/samples/"camera streaming"/sender/python ```
4. Update default python version from 2.7 to 3.6 (ROS 1 only works with python 2.7, but ZED python SDK only works with 3.6, so we need to switch back and forth.. ): ```sudo update-alternatives --config python ```
5. start ZED stereo video feed streaming: ```python streaming_sender.py```