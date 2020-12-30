# SFX-100-Modbus
GUI and Library to manage profiles for servo drivers used in SFX-100 actuators.

Available as Standalone Application or Simfeedback Extension.  

Please support this great project.  
https://opensfx.com

![SFB-Extension](doc/img/sfb-extension.jpg?raw=true|width=100px)

![SFB-Standalone](doc/img/modbus-standalone.jpg?raw=true|width=100px)

# Warning
You could make your actuators unsusable, unaccessible or maybe even brick or damage them irreversible.

The changes written by this tool affects the servo driver and its performance directly.
When wrong parameters are written it will result in unexpected behaviour of the drives. You may even get injured. 

No checks or rollbacks are provided to ensure the correctness of the profiles being read or written.

We are not responsible of any damages caused by usage of this application!

**You have been warned!**

# Installation  
Do not click on the green Download button on this page. This would download the sources only.  
Download the files from the releases page instead or by clicking links below.      
https://github.com/ashupp/SFX-100-Modbus/releases

[Download latest Simfeedback Modbus Extension Here](https://github.com/ashupp/SFX-100-Modbus/releases/latest/download/ModBusExtension.zip)  
[Download latest Standalone Modbus Tool Here](https://github.com/ashupp/SFX-100-Modbus/releases/latest/download/sfx-100-modbus-gui.zip) 

# Requirements

## Hardware
First of all you need the modbus enabled servo drives of the 90ST-M02430 set.

To connect the drives you need a USB to 485 adapter/USB to Modbus adapter.
  
There are plenty of adapters out there. Each of the following adapters have been checked and are working great.

* https://www.amazon.de/gp/product/B07L2VLY5D
* https://www.amazon.de/DIGITUS-Seriell-Konverter-Einbaubuchse-schwarz/dp/B007VZY4CW

## Servo drives
In order to use this program - the servo drives have to be preconfigured manually.  

Please see the following video on how the servo drives can be configured:  
https://opensfx.com/testing-and-configuring-servo-drives/

### Step 1 - Set the correct IDs

**Parameter: Pn065**  

Set the servo ID of each servo drive according to the following table.

Servo use           | ID to set
--------------------| -------------
Front right         | 1
Rear right          | 2
Rear left           | 3
Front left          | 4
Belt tensioner      | 5
Traction loss       | 6
Understeer          | 7
Acceleration sled   | 8

It is not a problem if you skip numbers. Its just important to have the corresponding IDs for servo use.

*Note: The servo use and ID of servo 1-4 are matching the wiring described in SFX-100 manual:  
https://opensfx.com/build-process-controller-wiring/*

### Step 2 - Set/Check the connection details

*Note: If you bought the servo drives with modbus support, the following parameters will probably be already set to the values. Anyway its better to double-check*

Parameter (Pnxxx)   | Value to set
--------------------| -------------
PN064               | 2
Pn066               | 5
Pn067               | 8

### Step 3 - Wiring
Connect the red cables to 485+/A+, the green cables to 485-/B- and the black cables to GND of the USB RS485 adapters.  
You can use Wago clamps to hook all same color wires together.  
Since it is a bus system all servo drives can be connected at the same time to the adapter.

![Prototype wiring](doc/img/prototype-wiring.jpg?raw=true|width=200)

### Step 4 - Launch the app
Even though the app is pretty much self-explanatory - details will follow.

### Step 5 (Optional - Advanced) - Extending the XML Files
You can edit the sfx-100-parameters.xml for your needs.  
More and custom parameters can be added. Just have a look at the existing parameters with its values (especially min/max/default).  
This is only for advanced use because you could brick your adapters with wrong settings and even injure yourself.  
So please take care.

# Third party Libraries and Thanks  
This library uses the EasyModbus Client library  
https://sourceforge.net/projects/easymodbustcp/

This tool is based on the research and proof of concept by SimFeedBack community member Dsl71.
