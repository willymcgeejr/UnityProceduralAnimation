# Unity Procedural Animation Project

Introduction
--
This is an extension of a research assignment I did for my graphics and animation course. It relies on [this](https://www.weaverdev.io/blog/bonehead-procedural-animation) introductory tutorial to procedural animation in Unity by WeaverDev and utilizes [this](https://assetstore.unity.com/packages/tools/animation/fast-ik-139972) very easy to use inverse kinematics package by Daniel Erdmann.

The tutorial was very informative and well written, but I wanted to know if this style of animation could be adapted to fit to the geometry underneath rather than just a static plane. It wasn't necessary for the assignment, but after I presented the first version of the demo, I wanted to give it a shot.

![Original animations with no geometry checking](https://github.com/willymcgeejr/UnityProceduralAnimation/blob/master/CMPT485ProceduralAnim/Assets/GIFs/30d0e5a4647c782cfd809138210255e3.gif)

As you can see above, our makeshift gecko does a great job of smoothly following the fly, but it just hovers at a static y-coordinate in worldspace. We can change that!


Positioning the Feet
--
The first thing I wanted to do was make sure the gecko's feet knew where they needed to be in the world at any given time. If we just translate the home position relative to the gecko, the IK targets will follow along on their next step.

By making a raycast downward from some position above the current 'home' position of the foot, we can use the point at which the raycast hits as a basis for where the new home should be. We can also use the normal of the same raycast to inform the rotation of the feet so they stay flat to the ground.

![Something about this doesn't look right!](https://github.com/willymcgeejr/UnityProceduralAnimation/blob/master/CMPT485ProceduralAnim/Assets/GIFs/003784803f43450edf215b3912e9a5d0.gif)

It works! But simply aligning the feet to the normal isn't good enough. If you look closely at the above GIF, the rotation of the feet is not being taken into account. Thankfully, because the feet are children of the overall gecko model we can just make sure that the local y-rotation of the foot is zeroed out after each adjustment to the normal.

![Better!](https://github.com/willymcgeejr/UnityProceduralAnimation/blob/master/CMPT485ProceduralAnim/Assets/GIFs/7288c861ec199add8aa589de5ab3258a.gif)

Much better!


Positioning the Body
--
The next problem we have to solve is the orientation of the rest of the gecko body. While the feet are adapting nicely to the ground below them, the body isn't following suit.

![Not quite what we're looking for](https://github.com/willymcgeejr/UnityProceduralAnimation/blob/master/CMPT485ProceduralAnim/Assets/GIFs/e06cc4b86726afdb93f3fb63176a19a8.gif)

There are a few ways we could fix this. Since we're already doing the calculations, I opted to just take the average of the normals from each foot to give me a new normal that we want to try and fit the body's "up" vector to.

The problem with doing this directly is you get a very choppy adjustment each time the normal of any of the feet change, as shown below.

![Uh oh!](https://github.com/willymcgeejr/UnityProceduralAnimation/blob/master/CMPT485ProceduralAnim/Assets/GIFs/2f8ed3c9a8e95a5d671930153acc4d68.gif)

To fix this, we can determine what rotation needs to be applied to fit the body to the average normal and then apply some smoothing between where the body is now and where we want it to be as seen in this block of code in GeckoController.cs:

>        // Figure out what rotations are needed to align the body with the combined feet normals,
>        // then apply a smoothing function to prevent the body from 'snapping' into the new rotation
>        float yRot = transform.eulerAngles.y;
>        Quaternion tempRotation = transform.rotation;
>        transform.rotation = Quaternion.LookRotation(transform.forward, bodyNormal);
>        transform.rotation = Quaternion.LookRotation(transform.right, bodyNormal);
>        transform.Rotate(0, yRot - transform.localRotation.eulerAngles.y, 0);
>        transform.rotation =
>            Quaternion.RotateTowards(tempRotation, transform.rotation, smoothingSpeed * Time.deltaTime);

![Image from Gyazo](https://github.com/willymcgeejr/UnityProceduralAnimation/blob/master/CMPT485ProceduralAnim/Assets/GIFs/860dad4389fab0b37dcad29ee51aea5e.gif)

This looks much more natural! Now things are working more like we were hoping they would from the start. It works nicely on flat inclines and curved surfaces (so long as the gecko doesn't fall off the edge)!

![Image from Gyazo](https://github.com/willymcgeejr/UnityProceduralAnimation/blob/master/CMPT485ProceduralAnim/Assets/GIFs/4539bfd6a6916b2bd8a77a936f7bcdd8.gif)

About the Included Code
--
The code has a script attached to the TargetObject that, when enabled, will automatically pilot the target object through the scene, focusing on how the gecko follows it on various paths over different terrain.

If you want to play around with it, you can disable this script and drag the TargetObject around the world and watch our gecko follow it around!

There are also many inspector variables on the Gecko GameObject that allow you to play with the movement speed, turn speed, etc.

Conclusion and Additional Resources
--
The way the included demo works will work for scenes where all of the geometry underneath the gecko keep the normals of the feet within 90 degrees of the world's y-axis. That is, if you were to make a giant curved surface like a globe, the gecko would have to remain on the northern hemisphere - any attempt to lead it South of the equator would not work properly. I'll work on version where the gecko is 'magnetized' to the geometry below and can walk upside-down on surfaces when I get a chance and post a link to it here.

As linked in the introduction, the tutorial that covers some of the base code for the prodecural animation on an arbitrary plane can be found [here](https://www.weaverdev.io/blog/bonehead-procedural-animation). The easing function I used for the feet to move from step-to-step can be found in a larger library of easing functions [here](https://gist.github.com/Fonserbc/3d31a25e87fdaa541ddf).

Rather than make my own inverse kinematics package I used a free package (FastIK) available on the Unity Store which can be found [here](https://assetstore.unity.com/packages/tools/animation/fast-ik-139972). If you don't know how inverse kinematics work, I highly recommend [An Introduction to Procedural Animations by Alan Zucconi](https://www.alanzucconi.com/2017/04/17/procedural-animations/).
