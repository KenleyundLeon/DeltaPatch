# <div align="center">DeltaPatch - Automate Your Updates</div>
<div align="center">
<a href="https://github.com/northwood-studios/LabAPI"><img src="https://image2url.com/images/1759563390122-d4824ef5-f596-4c20-9063-2c606a16971c.png"></a>

Tired of updating all your plugins manually? Let's automate it!

<img src="https://image2url.com/images/1759611430674-abc9ea56-8150-475c-a673-24db66c2b634.png" style="width:200px; height:200px;"> 
</div>

## Quick Tutorial
<details>
  <summary>Show instructions</summary>

- Download the latest release file called: "DeltaPatch.dll"
- Copy the file to `.config/SCP Secret Laboratory/LabAPI/plugins/{port}` ← This part is important. DeltaPatch isn't ready for global usage yet.  
- Reboot the server. After rebooting, a config should appear at `.config/SCP Secret Laboratory/LabAPI/config/{port}/DeltaPatch/config.yml`.  
- You can customize how often it should check for updates and when the server should reboot (to apply the changes).  
- That’s it! All plugins that include this badge:  
  <a href="https://github.com/KenleyundLeon/DeltaPatch"><img src="https://image2url.com/images/1759565889245-ff2e02c2-1f19-4f72-bc06-43a3b77fb4bd.png" width="100" height="60"></a>  
  will be automatically updated to the latest release version.
</details>
  
## For Developers
<details>
  <summary>Show developer guide</summary>

### Adding Compatibility
- Adding DeltaPatch compatibility is easy.  
- Create a new public string value in your main file where the `Plugin` interface is used:  
  public string githubRepo = "CHANGE THIS TO YOUR GITHUB REPOSITORY";  
- Example image:  
  <img src="https://image2url.com/images/1759612903745-72d179ea-0dc5-4a45-93fd-efa463b5f760.png">

### Compatibility Badge
```html
<a href="https://github.com/KenleyundLeon/DeltaPatch"><img src="https://image2url.com/images/1759565889245-ff2e02c2-1f19-4f72-bc06-43a3b77fb4bd.png"></a>
```
USE ONLY IF YOUR PLUGIN IS COMPATIBLE!
</details>

## Licensing
This project is licensed under the **Business Source License (BSL)**.  
You are allowed to fork this repository and make your own integrations, **but you must give proper credit** to the original author.

## Credits
- [Northwood Studios](https://github.com/northwood-studios)  
- [LabAPI Team](https://github.com/northwood-studios/LabAPI)
