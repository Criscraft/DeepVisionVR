![Title](Panorama.png)

# A Walk in the Black-Box: Deep Convolutional Neural Network Visualization in Virtual Reality 

This repository visualizes deep convolutional neural networks (CNNs) in 3D. Take a walk in your CNN and interact with it to get a more intuitive understanding of Deep Learning or to analyze your models. The software provides visualization algorithms like feature visualization to reveal what visual concepts the models have learned. Virtual Reality and desktop mode are available. Currently, CNNs for image classification and segmentation are supported. 

The repository consists of the client and the server part. The client contains the Unity project to display and to interact with the networks. The server is a Python based implementation, handles the networks and provides the client with data. The server can be run in Docker and an appropriate Dockerfile is included in the repository. 


## Requirements

For the client:
Unity 2021.1.14f1

For the server:
Docker and NVIDIA-docker
Use the Dockerfile to build your Docker image.


## Get started with the demo

Follow the instructions to set up the [Server](https://github.com/Criscraft/DeepVisionVRServer) and the [Client](https://github.com/Criscraft/DeepVisionVRClient). Start the server first and then start the client to take a walk in the black-box.


## How to visualize your own CNN

To visualize your own networks and datasets you will have to modify some scripts on the server side. Go to the server directory and create a new project folder in DeepVisionVR/DeepLearningServer/Projects/ 
To get started, copy the Demo project and replace the network or the datset. Currently, the software expects the networks to written in the Pytorch framework. 
Check out the other projects to see what the visualization software can be used for. 


## Results

![Network](Netzwerk.png)
![Architektur](Architektur.png)
![Feature Visualisierung](FeatureVisualisierung.png)