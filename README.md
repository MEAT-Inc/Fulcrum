# Fulcrum
Fulcrum is the ultime J2534 DLL Shim written in C++ which is able to pipe data out from the native calls to any other location for super deep diagnostic logging

---

### Using the Fulcrum

- Here's a little piece of software I made which allows me to inject data in real time into a running J2534 session for any of the OE software applications. It also provides me with usable debug logging output so I can review and modify any of my projects I build for you guys going forward.
- This is a super super early version of this app so bear with me when it comes to features on it. Once we gather a ton of data for our simulations, we can then build the PassThru testing app we've been talking about.
- To use this, Install the MSI packages linked in the releases tab of this repository. Then when you go to use your OE Application, pick the DLL titled "FulcrumShim". It will then pop up a new box which is where you pick your actual passthru device. The DLL simply slips between the OE Application and the PT interface you're using so I can see some more of the black magic that goes on between the apps and the VCIs. 

---

### Questions, Comments, Concerns? 
- I don't wanna hear it...
- But feel free to send an email to neo.smith@motorengineeringandtech.com. He might feel like being generous sometimes...
