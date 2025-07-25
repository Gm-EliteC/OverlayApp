# lineOverlay

Usage:
Compiled: You will need to place all of you compiled files in to the folder Resources in PangYaC (exluding the LineOverlay.pdb)
Debugging: Simply it run it before PangYaC. The Windows Forms app will attemp to start a nice instance if is not there. 
# LineOverlay - Windows Presentation Foundation (WPF)

This application is inspired by **XPangya** and integrates the **Acrisio Pangya Calculator** into a modern **Windows Forms** interface. The goal is to deliver a high-resolution, user-friendly tool for players of the online golf game **Pangya** to easily calculate and optimize their shots.

## 🎯 Features

- ⚙️ Integrates Acrisio's shot calculation logic
- 🖥️ Higher-resolution, resizable UI for modern displays
- 📐 Designed specifically for Pangya players who need accurate and fast shot estimations
- 🎮 Seamless desktop experience without needing a web browser
  
👉To see a Video tutorial go to https://www.youtube.com/watch?v=86KGSaSlN7Y 

## __What is Working:__

-Calculating  
-Support For Multiple Resolutions (3840x2160, 2560x1440, 2048x1152, 1920x1080, 1600x900) both screen mode and maximized.  
 &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;•Do not maximize low resolution game into a big monitor, just don't.  
-For best results use 1280x960, 2560x1140, 3840x2160 due to Spin. If you use other resolutions manually adjust the spin (because the app cannot locate a pixel in the decimal location).  
-Spin and Curve Cursor, Supports dragging the Screen.   
-AIM Assist, Supports Dragging the Screen (see supported resolutions).  
-Move UI elements freely.  
-Calculate with any spin  
-Wind Angle reading for ANY resolution. Just adjust it using the buttons (Up, Down, Left, Right, +, -)  and click "Save SS" (this will be saved to your computer app folder).  
 &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;•If left or right arrows are not moving, try clicking many times to left until it gets set up correctly, Save SS.  
-Configuration File closing, opening, installing. Now you can simply download, install it or run it from PangYaC.exe  
-Course Button (auto fills Height, Distance, Slope) BL, Ice Cannon, Ice Spa, Pink Wind, Lost Seaway only, will keep adding more. Now with colors and remembers course.   
-4k! strangely enough and due to the nature of the pixels Spin and Curve values are not prefect in odd numbers. So do 2, 4, 6,  etc.   
-For non supported resolutions, you can now view a log saved in the folder where you save and see more details so we can resolve.   
-Improved screen resolution reading and game capture so it accounts for Windows 10, 11 and the different borders.   
-Stay on Top, when taking screenshot, simply click stay on top to capture F11 in the app, not the game. Then un check to view wind angle reading.  
-All courses have been added under "select course"  
-Now you can check this and the program will try to find the best caliper and spin within 0.1y (so if is short 0.1 it can still work).  
-Life Quality updates   
-Minor bugs with overlay and added few missing slopes  


## __What is Not Implemented__
-Slope Reading
What Is Not Working Properly?  
-Some resolutions have issues and add a little curve, so try manual spin or other methods. Again, try 1280x960, 2560x1140 or 4K. 

## 🚀 Getting Started

To run the project locally:

1. Clone the repository:
```bash
git clone https://github.com/Gm-EliteC/OverlayApp.git
```

## 🙏 Credits

- **Acrisio's Pangya Calculator** – Core logic and formulas (I have slightly modified the original one) are based on Acrisio's original calculator (Java Script), a powerful tool for shot accuracy in Pangya.  
  > https://github.com/Acrisio-Filho/SuperSS-Dev

- **XPangya** – Visual inspiration for the UI/UX design that enhances shot calculation visibility and usability.

- **Pangya Community** – For years of dedication, strategies, and gameplay insight that inspired the creation of this enhanced tool.

- Developed by **GM_EliteC**, 2025.

