# FulcrumInjector

![Fulcrum_HomePage](https://user-images.githubusercontent.com/62027458/176723420-7d2c7228-a247-44d5-8928-0f1bb5aa171d.PNG)

---

### FulcrumInjector Features
FulcrumInjector is the ultime J2534 DLL Shim written in C++ which is able to pipe data out from the native calls to any other location for super deep diagnostic logging.  This application has a TON of useful features in it. Some of the most notable ones are listed below. Not all features are complete at this time but all of the layouts for them are more than 90% complete on the UI side of things.
- Full shim logging between a PassThru interface and my application
- Dynamic realtime processing of log output to build simulations that can be replayed at any time using this app. (this is a WIP. It's like 40% done. I can build the sims but I can't replay them yet.)
- Easy access to booting any OE app on a machine. This works for your Unicorn boxes as well. 
- Full diagnostic J2534 information about any PassThru interface connected to a machine. If you don't see it listed in my app, then the head is broken. I'll bet big money on that.
- Integrated session reporting to send me feedback or logs directly for reviewing. These emails are also automated so many times if it's an email with just logs, a simulation is built from the files attached to it. 
- Settings configuration to tweak this app and it's behavior for different OEs. I've got it dialed in pretty well at this point but sometimes there's small changes that need to be made so you can do all of that without having to restart a diagnostic session.
- And lastly, a debug log viewing window which is color coded, automatically refreshing, and supports regex searching for the fancy fancy people out there who know what they're looking for in the log files.

---

### Using the FulcrumInjector

- Here's a little piece of software I made which allows me to inject data in real time into a running J2534 session for any of the OE software applications. It also provides me with usable debug logging output so I can review and modify any of my projects I build for you guys going forward.
- This is a super super early version of this app so bear with me when it comes to features on it. Once we gather a ton of data for our simulations, we can then build the PassThru testing app we've been talking about.
- To use this, Install the MSI packages linked in the releases tab of this repository. Then when you go to use your OE Application, pick the DLL titled "FulcrumShim". It will then pop up a new box which is where you pick your actual passthru device. The DLL simply slips between the OE Application and the PT interface you're using so I can see some more of the black magic that goes on between the apps and the VCIs. 

---

### Questions, Comments, Concerns? 
- I don't wanna hear it...
- But feel free to send an email to neo.smith@meatinc.autos. He might feel like being generous sometimes...
- Or if you're feeling like a good little nerd, make an issue on this repo's project and I'll take a peek at it.

--- 

### Screenshots of The FulcrumInjector

![Fulcrum_DllOutput](https://user-images.githubusercontent.com/62027458/176723498-299025eb-eb6d-4365-a10d-f31fd8e51e3d.PNG)

![Fulcrum_LogReview](https://user-images.githubusercontent.com/62027458/176723533-0199480c-3dfb-4f36-b0c3-c96ab5775429.PNG)

![Fulcrum_SimPlayback](https://user-images.githubusercontent.com/62027458/176723582-7863910b-5ebe-43f4-a209-8bc1816e3281.PNG)

![Fulcrum_EmailReport](https://user-images.githubusercontent.com/62027458/176723614-c6aefdc3-9579-4130-8533-60f46ebcd18b.PNG)

![Fulcrum_SettingsPage](https://user-images.githubusercontent.com/62027458/176723657-ea7cb9aa-db55-48dd-b6a2-de9d841fd3ed.PNG)

![Fulcrum_DebuggingView](https://user-images.githubusercontent.com/62027458/176723698-51e22e4c-8d9c-4120-8faa-e043050489bb.PNG)

![Fulcrum_AboutView](https://user-images.githubusercontent.com/62027458/176723734-6742fce9-31bc-47fc-b69a-809d9e508929.PNG)
