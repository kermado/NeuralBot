# Neural Network Aimbot

This is a general purpose aimbot, which uses a neural network for enemy/target detection.

## Features

Provided below is a list of the main features:

* Efficient screen capturing
* GPU-accelerated neural network inference for enemy/target detection (NVIDIA GPUs only)
* Object tracking via Hungarian Algoritm
* Multiple aiming methods
* Trigger bot
* ESP overlay
* Save/load configurations

## Components

The aimbot is designed to be extended/customised. You can implement your own components and they can be selected and configured through the user interface. This section details the major components. The aimbot includes some basic implementations of these components by default.

### Screen Grabber

The screen grabber is responsible for capturing a specific region of the screen. The included GDI screen grabber seems to work well in practice.

### Object Detection

The object detector is responsible for identifying potential targets from an image. The included detector uses the OpenCV library to perform object detection using a neural network. You must provide a configuration (.cfg) file and associated model (.weights) file in the Darknet format. Note that the aimbot requires the object detection to run at a high framerate (> 100fps) for good results. The YOLOv4 architecture is recommended with a input resolution of 512x512. It is possible to use a larger screen capture area, though the accuracy of the trained neural network may be adversely affected. The included OpenCV detector supports NVIDIA Pascal and Turing cards only.

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
