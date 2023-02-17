# FulcrumInjector

![Fulcrum_HomePage](https://user-images.githubusercontent.com/62027458/176723420-7d2c7228-a247-44d5-8928-0f1bb5aa171d.PNG)

---

## FulcrumInjector Features
FulcrumInjector is the ultimate J2534 DLL Shim written in C++ which is able to pipe data out from the native calls to any other location for super deep diagnostic logging.  This application has a TON of useful features in it. Some of the most notable ones are listed below. Not all features are complete at this time but all of the layouts for them are more than 90% complete on the UI side of things.
- Full shim logging between a PassThru interface and my application
- Dynamic realtime processing of log output to build simulations that can be replayed at any time using this app.
- Easy access to booting any OE app on a machine. This works for your Unicorn boxes as well. 
- Full diagnostic J2534 information about any PassThru interface connected to a machine. If you don't see it listed in my app, then the head is broken. I'll bet big money on that.
- Integrated session reporting to send me feedback or logs directly for reviewing. These emails are also automated so many times if it's an email with just logs, a simulation is built from the files attached to it. 
- Settings configuration to tweak this app and it's behavior for different OEs. I've got it dialed in pretty well at this point but sometimes there's small changes that need to be made so you can do all of that without having to restart a diagnostic session.
- The ability to import, parse, and convert J2534 logs from ANY J2534 interface. This means we can take a log from a previous scan session in a car, load it into this app, and the generate a simulation which can be played back using this application
- And lastly, a debug log viewing window which is color coded, automatically refreshing, and supports regex searching for the fancy fancy people out there who know what they're looking for in the log files.

---

## Using the FulcrumInjector

- Here's a little piece of software I made which allows me to inject data in real time into a running J2534 session for any of the OE software applications. It also provides me with usable debug logging output so I can review and modify any of my projects I build for you guys going forward.
- This is a super super early version of this app so bear with me when it comes to features on it. Once we gather a ton of data for our simulations, we can then build the PassThru testing app we've been talking about.
- To use this, Install the MSI packages linked in the releases tab of this repository. Then when you go to use your OE Application, pick the DLL titled "FulcrumShim". It will then pop up a new box which is where you pick your actual passthru device. The DLL simply slips between the OE Application and the PT interface you're using so I can see some more of the black magic that goes on between the apps and the VCIs. 

--- 

## Configuring J2534 Hardware
- This section will go over in detail how to setup your PassThru devices for all the different features of this application
- There's three main hardware configuration types which we can follow. Each of these possible hardware configurations are shown below with a brief overview of how to use the FulcrumInjector application for each poossible configuration. 
  - Sniffing an OE Application
  - Sniffing an external device
  - Simulation Playback

### *Sniffing OE Applications*
- This setup is used when you wish to sniff/shim the data being transmitted from an OE application to any J2534 interface connected to the OE application of your choice. 
  - To set this up, you need the following equipment
  - A J2534 PassThru interface (I'll be explaining how to do this with a CarDAQ-Plus 3)
  - A laptop which has the OE Application you wish to sniff and a version of the FulcrumInjector software
  - A vehicle which is compatible with the OE application you're using.
- Once you've got all the afformentioned parts in place and ready to go, you need to setup your hardware in the following configuration. Follow this diagram as close as possilbe to ensure the best results during sniffing routines.

    <p align="center">
    <img src="https://user-images.githubusercontent.com/62027458/188881321-9f759fe4-057e-47ff-a036-5dea3a080e60.PNG">
    </p>
    
- From this point, here's what you do to sniff the communication data between the car and the OE Application.
  - Open the FulcrumInjector application and using the left sidebar, find the OE Application you wish to control. Once found, click "Launch OE Application under the list of app entries. This will open up the selected OE application.
  - **NOTE:** If you're using a CarDAQ-Plus 3, ignore the following and go to the next step
      - Inside the FulcrumInjector, locate the settings gear in the main view navigation bar and click it.
      - Once there, find the settings named *Allow Selection Box Popup* and ensure it is checked. 
      - This allows the selection box of the FulcrumShim to appear so you can pick which device you wish to use for this session. 
      - If you want, you can modify the value of the setting called *Default Injector DLL* to set a forced default DLL to use. 
          - For the CarDAQ-Plus 3, this value is normally `C:\\Program Files (x86)\\Drew Technologies, Inc\\J2534\\CarDAQ Plus 3\\cardaqplus3_0404_32.dll`.
          -  For all other DLLs or devices, you need to locate the DLL of the device on your machine and copy the path to it. This can be found easily by finding the DLL entry in the Registry Editor app and finding the value of the key named "Function Library".
      -  Once you've set these settings values, press the save button at the top of the page. The settings should save automatically, but just click it to be save. 
  - When the OE Application is open, you need to navigate to the device setup window/selection window of that application. Most times, this is sotred somewhere under the "Setup" or "Settings" menu of the OE application itself. Once found, under your device selection, you need to pick "FulcrumShim" as your device type and press confirm to set the device type. 
  - Now, if you go back into the FulcrumInjector application and navigate to the second menu entry named *Injector DLL Output*, you should see some output in the text viewer. If there's not, the two pipe state status boxes should at least have one that says *Connected*. If they don't, it does not mean something isn't working, but rather it means the OE App just hasn't accessed your PassThru device yet. 
  - From here, just scan the vehicle or do whatever routines you wish to sniff/shim out. During these routines, the output inside the FulcrumInjector should update in real time as the OE application performs actions on the vehicle. 
  - Once you're done using the OE app, you can close it from inside the OE app itself, or by going back to the FulcrumInjector and clicking "Terminate OE Application" where the launch button used to be. 
  - To pull the log file that was built while using the OE application, you can either navigate to the path shown in the top of the log output viewer, or by going into `C:\Program Files (x86)\MEAT Inc\FulcrumShim\FulcrumLogs\` and finding the newest log file named `FulcrumShim_Logging_XXXXXXX.shimLog` where the X values would be the date and time the log was built.

### *Sniffing External Devices*
- This setup is used when you wish to sniff/shim the data being transmitted from a third party interface connected to a vehicle that is NOT using an OE application.
- To set this up, you need the following equipment
  - A J2534 PassThru interface (I'll be explaining how to do this with a CarDAQ-Plus 3)
  - A laptop which has a version of the FulcrumInjector software
  - A vehicle which is compatible with the OE application you're using.
  - The third party diagnostic tool
  - An ODB2 Y Cable
  - The J2534 Bus Analysis Tool (I'm working on removing this as a requirement but that's quite far down the road)
    - Link to this tool: https://www.drewtech.com/downloads/tools/J2534-1Tool-0404_Installer_v1_0_14.msi
- Once you've got all the afformentioned parts in place and ready to go, you need to setup your hardware in the following configuration.

    <p align="center">
    <img src="https://user-images.githubusercontent.com/62027458/189118065-a7a201d4-225c-42b9-af9c-aa26298f2ec1.PNG">
    </p>
    
- After setting up your laptop, passthru device, and third party interface as explained above, follow these steps to setup a sniffing routine.
    - **INSERT SNIFFING SETUP INSTRUCTIONS HERE**

### *Simulation Playback*
- This setup is used when you wish to simulate a built simulation file from inside the FulcrumInjector. These simulation files are used to play a CarDAQ-Plus 3 (or other device) as a vehicle, against another device as the diagnostic interface.
- To set this up, you need the following equipment
  - **TWO** J2534 PassThru interfaces (I'll be explaining how to do this with a CarDAQ-Plus 3)
  - A laptop which has a version of the FulcrumInjector software
  - An ODB2 Y Cable
  - Either a 120 ohm resistor or a CAN Bus terminating block
  - A built simulation file which will be used as our "vehicle"
- Once you've got all the afformentioned parts in place and ready to go, you need to setup your hardware in the following configuration.
    
    <p align="center">
    <img src="https://user-images.githubusercontent.com/62027458/188881913-31783fbe-45af-4539-9a9e-034c3b1bd5a7.PNG">
    </p>

- After setting up your laptop, passthru device, and third party interface as explained above, follow these steps to setup simulation playback routine.
    - **INSERT SIMULATION SETUP INSTRUCTIONS HERE**

---


## Development Setup
- NOTE: As of 2/17/2023 - I've closed down the readonly access for anyone to use. If you want to use these packages, please contact zack.walsh@meatinc.autos for an API key, and someone will walk you through getting into this package repository. This decision was made after realizing that while the key was readonly and on a dedicated bot account, it's not the best idea to leave API keys exposed. And since making these projects public, it was only logical to remove the keys from here.
- If you're looking to help develop this project, you'll need to add the NuGet server for the MEAT Inc workspace into your nuget configuration. 
- To do so, navigate to your AppData\Roaming folder (You can do this by opening windows explorer and clicking the top path bar and typing %appdata%)
- Now find the folder named NuGet and open the file named NuGet.config
- Inside this file, under packageSources, you need to add a new source. Insert the following line into here 
     ```XML 
      <add key="MEAT-Inc" value="https://nuget.pkg.github.com/MEAT-Inc/index.json/" protocolVersion="3" />
    ```
- Once added in, scroll down to packageSourceCredentials (if it's not there, just make a new section for it)
- Inside this section, put the following block of code into it.
   ```XML
    <MEAT-Inc>
       <add key="Username" value="meatincreporting" />
       <add key="ClearTextPassword" value="{INSERT_API_KEY_HERE}" />
    </MEAT-Inc>
    ```
 - Once added in, save this file and close it out. 
 - Your NuGet.config should look something like this. This will allow you to access the packages inside the MEAT Inc repo/workspaces to be able to build the solution.
    ```XML
      <?xml version="1.0" encoding="utf-8"?>
          <configuration>
              <packageSources>
                  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
                  <add key="MEAT-Inc" value="https://nuget.pkg.github.com/MEAT-Inc/index.json/" protocolVersion="3" />
              </packageSources>
              <packageSourceCredentials>
                  <MEAT-Inc>
                      <add key="Username" value="meatincreporting" />
                      <add key="ClearTextPassword" value="{INSERT_API_KEY_HERE}" />
                  </MEAT-Inc>
              </packageSourceCredentials>
              <packageRestore>
                  <add key="enabled" value="True" />
                  <add key="automatic" value="True" />
              </packageRestore>
              <bindingRedirects>
                  <add key="skip" value="False" />
              </bindingRedirects>
              <packageManagement>
                  <add key="format" value="1" />
                  <add key="disabled" value="True" />
              </packageManagement>
          </configuration> 
- On top of this, you'll also need to make sure that you've got the latest version of the WixInstaller toolkit and the WixInstaller extension for whatever version of VS you're using. 
- There's tons of guides how to do this online so I won't be going over it here.

---

## Questions, Comments, Concerns? 
- I don't wanna hear it...
- But feel free to send an email to zack.walsh@meatinc.autos. He might feel like being generous sometimes...
- Or if you're feeling like a good little nerd, make an issue on this repo's project and I'll take a peek at it.

--- 

## Screenshots of The FulcrumInjector

![Fulcrum_DllOutput](https://user-images.githubusercontent.com/62027458/176723498-299025eb-eb6d-4365-a10d-f31fd8e51e3d.PNG)

![Fulcrum_LogReview](https://user-images.githubusercontent.com/62027458/176723533-0199480c-3dfb-4f36-b0c3-c96ab5775429.PNG)

![Fulcrum_SimPlayback](https://user-images.githubusercontent.com/62027458/176723582-7863910b-5ebe-43f4-a209-8bc1816e3281.PNG)

![Fulcrum_EmailReport](https://user-images.githubusercontent.com/62027458/176723614-c6aefdc3-9579-4130-8533-60f46ebcd18b.PNG)

![Fulcrum_SettingsPage](https://user-images.githubusercontent.com/62027458/176723657-ea7cb9aa-db55-48dd-b6a2-de9d841fd3ed.PNG)

![Fulcrum_DebuggingView](https://user-images.githubusercontent.com/62027458/176723698-51e22e4c-8d9c-4120-8faa-e043050489bb.PNG)

![Fulcrum_AboutView](https://user-images.githubusercontent.com/62027458/176723734-6742fce9-31bc-47fc-b69a-809d9e508929.PNG)
