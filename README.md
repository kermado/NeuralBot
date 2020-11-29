# NeuralBot - Neural Network Aimbot

This is a general purpose aimbot, which uses a neural network for enemy/target detection. The aimbot doesn't read/write memory from/to the target process. It is essentially a "pixel bot", designed primarily for use with first-person shooter games. Please check that the user agreement for your game allows the use of such a programs!

![AimBot](gui.PNG?raw=true "AimBot")

This aimbot achieved the top score in [Aim Lab](https://youtu.be/vtTsFcAslbE), with the three layer yolov4-tiny convolutional neural network trained on less than 300 images.

## Features

Provided below is a list of the main features:

* Efficient screen capturing
* GPU-accelerated neural network inference for enemy/target detection (NVIDIA GPUs only)
* Object tracking via Hungarian Algoritm
* Multiple aiming methods
* Trigger bot
* ESP overlay
* Save/load configurations

## Requirements

Unless you are downloading a pre-built release, you must install the following requirements in order to use the included OpenCV detector:

* [OpenCV 4.4.0](https://opencv.org/releases/)
* [NVIDIA CUDA Toolkit 10.0](https://developer.nvidia.com/cuda-toolkit-archive)
* [NVIDIA cuDNN 7.0](https://developer.nvidia.com/cuda-toolkit-archive)

Please see the [cuDNN installation instructions](https://docs.nvidia.com/deeplearning/cudnn/install-guide/index.html#installwindows). Note that the pre-built releases include all the necessary DLL dependencies.

## Components

The aimbot is designed to be extended/customised. You can implement your own components and they can be selected and configured through the user interface. This section details the major components. The aimbot includes some basic implementations of these components by default.

### Screen Grabber

The screen grabber is responsible for capturing a specific region of the screen. To screen grabbers are included:

#### GDI Grabber

The GDI screen grabber uses the Win32 GDI API. This seems to be pretty efficient in practice, capturing the main window for the target process.

#### DirectX Grabber

The DirectX screen grabber uses the Windows Desktop Duplication API. This is supposedly the most efficient method, though the GDI method appears to be more efficient in practice. One important difference is that this method captures from the screen rather than the process main window. You may therefore need to disable ESP rendering in order to avoid confusing the neural network.

A pre-built binary for the DirectX grabber is included, though the complete [source code](https://github.com/kermado/DXGrabber) is available.

### Object Detection

The object detector is responsible for identifying potential targets from an image. The included detector uses the OpenCV library to perform object detection using a neural network. You must provide a configuration (.cfg) file and associated model (.weights) file in the Darknet format. Note that the aimbot requires the object detection to run at a high framerate (> 100fps) for good results. The YOLOv4 architecture is recommended with a input resolution of 512x512. It is possible to use a larger screen capture area, though the accuracy of the trained neural network may be adversely affected. The included pre-built OpenCV detector supports NVIDIA Pascal and Turing cards only, though the complete [source code](https://github.com/kermado/Detector) is available.

### Object Tracker

The object tracker is responsible for assigning a unique ID to each object. The purpose is to allow the aimbot to continue tracking the same target without switching to other nearby targets. The included tracker uses the Hungarian Algorithm with a GIOU metric. It allows objects to lose detection for a configurable period of time without switching to another target.

### Target Selector

The target selector is responsible for selecting which object should be targeted. The included selector uses a simple distance metric for this purpose.

### Target Aimer

The targget aimer is responsible for moving the crosshair towards the target. Three different aimers are included:

#### Flick Aimer

The flick aimer smoothly acquires the position of the target and smoothly moves the crosshair towards that position. The position is not updated over time so as to track any movements that the target makes. If calibrated correctly, the flick aimer should always hit a stationary target. It also shouldn't suffer from dropped detections, since the position is acquired once. The obvious disadvantage is that it is likely to miss a quickly moving target unless the speed is set to a very high value.

#### Feedback Aimer

The feedback aimer uses feedback control logic to continuously move the crosshair towards the target. You can configure this aimer to use Proportional (P), Proportional Integral (PI) or Proportional Integral Derivative (PID) control. Whilst this method allows tracking moving objects quite well, it usually tracks slightly behind fast moving objects. It may also be difficult to tune the control parameters.

#### Hybrid Aimer

The hybrid aimer combines features of the other two aimers. It uses simple proportional feedback control to get close to the target. Once the target is sufficiently close, it performs a final flick in order to increase the probability of hitting the target. This method is most suitable for low rate of fire weapons, such as sniper rifles.

### Trigger Bot

The trigger bot can automate the process of clicking a mouse button. The included trigger bot can be configured to click the mouse button when the target is sufficiently close. It can be used stand-alone, without any aim assistance.

### Mouse Injector

The mouse injector is responsible for injecting mouse events requested by either the aimer or the trigger bot.

## Limitations

### Image Scaling

If the screen capture region is not the same size as the neural network input then the image must be scaled. The current scaling method does not preserve aspect ratio. It is recommended that you set the capture region dimensions to be equal to the neural network input dimensions. This generally yields the best results. If you must use different capture region dimensions then it is highly recommended to maintain the aspect ratio to be equal to the neural network input aspect ratio.

### Fullscreen

The screen grabbers are unable to capture from most games that run in fullscreen mode. You will need to run the game in windowed or borderless windowed mode.

## Frequently Asked Questions

**Will this work with game xyz?**
This aimbot will probably work with most first-person shooter games. You will most likely need to configure the aimer for your mouse sensitivity in order to get the best results.

**Where can I find trained neural network configuration and model files?**
The aimbot doesn't include any such files at this time. You can always train your own neural network. The [Darknet](https://github.com/AlexeyAB/darknet) project describes how to do this.

**Will I get banned for using this?**
If the user agreement doesn't allow the use of such programs then probably. Many anti-cheat programs detect injected mouse input events. There is also a good chance that this will become "signature detected" by many anti-cheat programs in the future.

**How can I calibrate the aimers?**
The sensitivity settings can be calibrated in-game by using the flick aimer. Adjust the parameters until the flick lands perfectly on the target position. The PID parameters can be tuned using well-known methods, such as the Ziegler-Nichols method.

**Does this work with AMD GPUs?**
No. The included OpenCV binary has been built for NVIDIA Pascal and Turing GPUs only. An NVIDIA GTX 1080 or better is recommended.

**How do I run the aimbot?**
You will need to use Visual Studio 2019 or later to build the solution. Pre-built binaries may be added in the future.
