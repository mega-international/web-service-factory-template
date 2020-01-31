# Web Service Factory Template
This add-on allows you to build you own custom web service on the HOPEX platform.

## Getting started

If this is the first time you use this add-on we recommend you to execute the VSIX file to register the elment in Visual Studio 2019. 

### Prerequisites
This add-on in program in C# for the web front end part.

### Installation

#### Developping you code

1. Launch the Mega.WebServiceTemplate.Extension.vsix to add all the necessary element in Visual Studio 2019
2.  Launch Visual Studio and select the newly created element
3. In HOPEX create a macro (JAVA or C#)
3. Reference in the source contains in Visual Studio the HexaIdAbs of the macro stored in HOPEX
4. Developed the logic you need
5. Build you project

#### Deploy your web service

1. Make sure the macro is stored in the HOPEX environment and that the compiled version of this macro is available in the installation of HOPEX (jar for JAVA or dll for C#)
2. Deploy the builded Visual project in IIS
3. Convert this folder to an application in IIS with the application pool HOPEXAPI


## Use your web service

Make a call to you newly deployed REST API with an HTTP request. You can use a tool such as postman.

### Required information

Get this information from your adminsitrator in order to be able to make call to the API
- Environment absolute identifier
- Repository absolute identifier
- Profil absolute identifier
- HOPEX user login
- HOPEX user password

## Build instruction

The  web part should be built with Visual Studio 2019 or above. Make sure you use the latest version of the .net framework and that this framework will be available on the server you deploy your web service.

## Authors

* **Olivier GUIMARD** - *Product Owner* - [OGD](https://github.com/oguimardmega "OGD")
* **Sebastien CRONIER** - *Lead Dev* 
