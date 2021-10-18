![Title](Panorama.png)

# DeepVisionVR - Interactive 3D Visualization of Deep Neural Networks

This repository is the implementation of DeepVisionVR. It serves as a tool to inspect the activations in deep neural networks in three dimensions. With this tool you can take a walk in your image processing Convolutional Neural Network (CNN) and get a more intuitive understanding of what it is doing and how it processes information. It provides visualization algorithms like feature visualization to reveal  what visual concepts it has learned. Currently, it can display CNNs for image classification and segmentation. VR Support is available. The repository contains the Unity project to display the networks, the Python based implementation of the networks themselves and a Docker file to create the Python environment. Soon an easy demo will be published to make it easy to get started.


## Requirements

For the Unity Project:
Unity 2021.1.14f1

For the Deep Learining part:
Install Docker and NVIDIA-docker
Build the Docker image.

It is advised to run server and client on different machines due to GPU memory limitations.


## Results

![Network](Netzwerk.png)
![Architektur](Architektur.png)
![Feature Visualisierung](FeatureVisualisierung.png)