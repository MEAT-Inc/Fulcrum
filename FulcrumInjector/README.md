# **The FulcrumInjector Source Code**

This README will go over in detail each of the directories inside this folder and explain why each of them exists and how they work. At a high level, there's four main folders which are described below

This document will be updated as new class files are added to this application but for the time being, it will likely remain as it. Expect changes to be published here on every merge back into main for a release version

---

- **FulcrumLogic**
  - Contains all the supporting logic that is not directly realted to the MVVM pattern for this application
  - Houses all logic for Pipe Operations, Expression Generation, Simulation Building, PassThru logic, JSON control, and a set of extension classes which are used somewhat globally throughout this application
  - These directories are broken down below
  - There's also a file named FulcurmEmailBroker which is responsible for sending out email reports from the support view of the Injector application
  - **FulcrumPipes**
    - Holds all the pipe operation logic used for shimming the FulcrumShim DLL
    - Home to the PipeReader, PipeWriter, and a set of pipe event objects which are fired when pipes process data in the background of this application
  - **JsonLogic**
    - This folder contains all the JSON value loading logic used for locating values from within settings .json files in the injector application. 
    - Also contains any needed custom JSON Converters which are used for both the OE app listings, and the Settings Entries. 
    - The converters are used to allow for use of enums and auto generated property values when items are pulled from the settings .json file
  - **PassThruLogic**
    - This directory is broken down into three main categories. Simulations, Expressions, and Win32 Logic. 
    - The Expressions objects used to build expressions files are found within the subdirectory ExpressionObjects. See the base type of PassThruExpression for more information about this
    - The simulation generation routines and helper logic are stored inside the PassThruSimulation directory
    - There's only two classes related to simulation generation since most of the heavy lifting is done during expression conversion routines
    - Lastly, the Win32 Logic is used to pInvoke actions onto our shim DLL from within the Injector application.
    - We have a setup class which houses the Win32 API Calls natively, and a Logging class which maps out the WriteToLog functions inside the Shim DLL itself.
- **FulcumResources**
  - This folder holds a set of resource files which are used at runtime by the application for configuration
  - Contains the following files:
    - Unicorn Software List - Used to identify installed software on a Unicorn for the OE apps view
    - Fulcrum Shim DLL Configuration - A confiuration file which is used to help allow us to toggle Shim functions on the fly
    - Icons for the About view (MEAT INC Turbo) and for the Injector Toolbox (Application Icon file)
- **FulcrumViewContent**
  - Inside the view content directory, we have all the logical configuration and view controls for the MVVM layout of the Injector application
  - Within this folder we have three subdirectories named Models, ViewModels, and Views. Each of them is used to lay out one of the components of the MVVM scheme. 
  - We also have the FuclrumConstants file which is a set of staticly accessed view components that can be called or modified from anywhere on the application
  - **Models**
    - Holds a set of folders and classes which define a lot of the objects we're binding our UI contents onto.
    - The two main classes inside of this folder we need to keep in mind are the FulcrumNavMenuItem class and the SingletonContentControl class
      - These two classes are used to build the main hamburger layout for our application.
      - The menu item class defines the behavior for each menu item listed on our main hamburger view
      - The Singleton control class is used to ensure only one instance of each view control is built at runtime so we don't keep clearing out contents when the user changes tabs on the hamburger
    - **EventModels**
      - Contains event model information for actions processed in the background. 
      - Holds only a Device changed event model for background refreshing information when picking J2534 hardware
    - **PassThruModels**
      - Holds two classes used to help lay out the regex information needed for our Expressions and Synyax formatting routines
    - **SettingsModels**
      - Holds classes which store our static settings contents, the defintion of a setting object, and a collection of settings objects
      - These are used when reading or writing settings values to or from our app settings .json file
    - **SimulationModels**
      - Models used to help process events during simulation playback.
      - Contains definitions for a channel changed event, message processed event, and the base class type for for the previously mentioned classes 
  - **ViewModels**
    - This folder and the Views folder share a similar layout
    - They're broken down by view model 'type'. Meaning what the view model is used in conjunction with
    - There's a folder for the 'Core' View models which are view models used by the main menu entries on the hamburger menu and a 'Option' view model folder which is used for option entries on the hamburger menu (On the bottom of the menu bar)
    - The other files inside this folder are view models which are not related to the main hamburger menu. These include view models for DLL testing, pipe operations, installed OE apps, and the ViewModelControlBase class which is used to automatically wrap the INotifyPropertyChanged events into one base class to keep view model code strictly related to property control and action logic.
  - **Views**
    - Inside the views folder we have the same structure as the ViewModels folder but without the extra logic classes.
    - There's a folder for the 'Core' views which are views used by the main menu entries on the hamburger menu and a 'Option' views folder which is used for option entries on the hamburger menu (On the bottom of the menu bar)
- **FulcrumViewSupport**
  - **More information on this direcotry will be built later on.**
  - All this folder contains is the seemingly ENDLESS number of converters for XAML bindings, style sheets, animation configurations, and some of the syntax configuration classes used to highlight output in any of the many text viewers inside the Injector application
  - This folder also houses a set of Avalon Edit format helpers which are used in those same text editors. These files are really redundant and should eventually be cleaned up but it works for now so it's staying as is.