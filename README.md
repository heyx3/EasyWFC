# EasyWFC

A much easier-to-read port of the C# [Wave Function Collapse](https://github.com/mxgmn/WaveFunctionCollapse) texture generation algorithm, plus a WPF GUI for watching it in action. The project was made with Visual Studio 2015.

If you want a deep explanation of the idea behind the algorithm, check out the original WFC project linked above. In a nutshell, the program takes an input image and creates an output image that has the same kind of features. More specifically, any NxM piece of the output image (where N and M are chosen by you) must also appear somewhere in the input image.

I should note that I wrote this code from scratch instead of rewriting the original project, so while I think my approach is correct, it may differ significantly from the original. I tried reverse-engineering the algorithm based on the readme, the included GIFs, and the comments people have made about it.

## Purpose

The main goals of this project are:

1. Provide an implementation of the algorithm that is very easy for people to read and understand -- the original project had a great readme but the code left a lot to be desired! This project's code should be heavily-commented, split into multiple files, and have long descriptive variable/function names. It should also cut out the Quantum Mechanics terms in favor of more concrete ones. Additionally, the actual algorithm code should be kept totally separate from the GUI and I/O code.

2. Provide a simple GUI for the generation algorithm so you can see what it does, step by step, and tweak some settings as you go.

## Features

One thing my project doesn't do is the "simple tiled model". I chose to only go with one form of input in order to keep the code as simple as possible.

Another thing I'm missing is the ability to "ground" output textures when they're supposed to have a certain bottom (like the flower and city images in the original project).

I also *added* a feature that I found from the internet: if the algorithm finds a pixel that can't be "solved", then instead of giving up it can instead wipe out an area around that pixel so it can try filling it in again. The amount of area to wipe out is a certain multiple of the size of the input patterns. That multiple can be changed in the GUI. If you set the multiple to anything less than 1, it will give up instead of clearing the space.

## UI

There are two windows in the project: the "Main Window", where you set up the input and RNG seed, and the "Generator Window", where you run the generator. Assuming you're familiar with how WFC works, the behavior of these windows should be pretty easy to figure out. You can choose to make the input periodic along either axis, make the *output* periodic along either axis, and include mirrored/rotated versions of the input alongside the original.

## Code

All the actual code for the algorithm is in the "EasyWFC/Generator" folder. You can easily drop that folder into any C# project you want; the only dependency it has (other than standard .NET stuff) is `System.Windows.Media.Color`, which is a trivial structure containing four bytes for RGBA color.

The algorithm is split into several classes/files:

* `Vector2i`: A 2D integer coordinate. This struct makes the algorithm much nicer to read. Alongside this class are two other important types:
    * `Vector2i.Iterator` allows you to `foreach` over a rectangular range of coordinates*.
    * The `Transformations` enum, including things like "Rotate90CW" or "MirrorX". Transformations are used when parsing an input image, to include mirrored/rotated versions of the image.
* `MyExtensions`: Extension methods, mostly extending 2D arrays to support use of my `Vector2i` struct. For example, you can get/set values using a `Vector2i` index, get the size of the array as a `Vector2i`, and get a `Vector2i.Iterator` for every index in the array.
* `Pattern`: An NxM grid of colors that appears in the input. This struct provides an important method, `DoesFit()`, that checks whether a given output image could contain this pattern at a specific position.
* `Input`: The input image, as a 2D array of `Color`s. This class pre-computes a list of all `Pattern`s in its constructor.
* `OutputPixel`: A single pixel in the output image. It has the following fields:
    * Its final, chosen color (which may or may not exist yet).
    * Its color for visualization purposes (which is equal to the final color if it exists).
    * A Dictionary that stores, for every color the pixel *could* become, the number of ways it could become that color. This is used to weight the RNG when choosing a color for the pixel.
* `State`: The state of the WFC algorithm. This is where the meat of the algorithm lies. It contains an `Input`, a 2D grid of `OutputPixel`, some tweakable algorithm settings, and an `Iterate()` method that runs one iteration of the WFC algorithm.

## License

All code is under the MIT license (a.k.a. do whatever you want with it).

````
Copyright 2017 William A Manning

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
````
