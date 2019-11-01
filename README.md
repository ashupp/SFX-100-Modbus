# SFX-100-Modbus
GUI and Library to manage profiles for servo drivers used in SFX-100 actuators.

Please support this great project.  
https://opensfx.com

This tool is based on the research and proof of concept by SimFeedBack community member Dsl71.

Right now this is also more a proof of concept than a usable application.

#Warning
You could make your actuators unsusable, unaccessible or maybe even brick or damage them irreversible.

The changes written by this tool affects the servo driver and its performance directly.
When wrong parameters are written it will result in unexpected behaviour of the drives. You may even get injured. 

I am not recommending to use this tool at the current stage of development unless you are know exactly what you are doing.

No checks or rollbacks are provided to ensure the correctness of the profiles being read or written.

We are not responsible of any damages caused by usage of this application!

**You have been warned!**


# Third party Libraries
This library uses the EasyModbus Client library
https://sourceforge.net/projects/easymodbustcp/
