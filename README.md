# Fulcrum
Fulcrum is the ultime J2534 DLL Shim written in C++ which is able to pipe data out from the native calls to any other location for super deep diagnostic logging

![Injector_HardwareOverview](https://user-images.githubusercontent.com/62027458/150359554-159cc76f-e989-417c-b052-9acab40f1718.PNG)

---

### Fulcrum Features
This application has a TON of useful features in it. Some of the most notable ones are listed below. Not all features are complete at this time but all of the layouts for them are more than 90% complete on the UI side of things.
- Full shim logging between a PassThru interface and my application
- Dynamic realtime processing of log output to build simulations that can be replayed at any time using this app. (this is a WIP. It's like 40% done. I can build the sims but I can't replay them yet.)
- Easy access to booting any OE app on a machine. This works for your Unicorn boxes as well. 
- Full diagnostic J2534 information about any PassThru interface connected to a machine. If you don't see it listed in my app, then the head is broken. I'll bet big money on that.
- Integrated session reporting to send me feedback or logs directly for reviewing. These emails are also automated so many times if it's an email with just logs, a simulation is built from the files attached to it. 
- Settings configuration to tweak this app and it's behavior for different OEs. I've got it dialed in pretty well at this point but sometimes there's small changes that need to be made so you can do all of that without having to restart a diagnostic session.
- And lastly, a debug log viewing window which is color coded, automatically refreshing, and supports regex searching for the fancy fancy people out there who know what they're looking for in the log files.

---

### Using the Fulcrum

- Here's a little piece of software I made which allows me to inject data in real time into a running J2534 session for any of the OE software applications. It also provides me with usable debug logging output so I can review and modify any of my projects I build for you guys going forward.
- This is a super super early version of this app so bear with me when it comes to features on it. Once we gather a ton of data for our simulations, we can then build the PassThru testing app we've been talking about.
- To use this, Install the MSI packages linked in the releases tab of this repository. Then when you go to use your OE Application, pick the DLL titled "FulcrumShim". It will then pop up a new box which is where you pick your actual passthru device. The DLL simply slips between the OE Application and the PT interface you're using so I can see some more of the black magic that goes on between the apps and the VCIs. 

---

### Questions, Comments, Concerns? 
- I don't wanna hear it...
- But feel free to send an email to neo.smith@motorengineeringandtech.com. He might feel like being generous sometimes...

--- 

### Screenshots of The Fulcrum
![Injector_DllLogging](https://user-images.githubusercontent.com/62027458/150359675-b8639413-fed9-4a25-84b3-8dba5ad96c50.PNG)
<br/>
![Injector_LogReviewing](https://user-images.githubusercontent.com/62027458/150359678-a0066a0f-980b-4a8d-a585-79cfb2dbd795.PNG)
<br/>
![Injector_SessionReporting](https://user-images.githubusercontent.com/62027458/150359682-0a0a3b61-5e89-48a3-ae08-b15339ad0999.PNG)
<br/>
![Injector_SettingsPage](https://user-images.githubusercontent.com/62027458/150359686-88da0940-78ea-4754-9121-41c4172a5844.PNG)
<br/>
![Injector_DebuggingView](https://user-images.githubusercontent.com/62027458/150359672-4788a044-b863-49be-9046-b387c4f6a0c8.PNG)
