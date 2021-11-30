##################################################### 
# Thjs script is used to upgrade the version number #
# of our DLL project when it is built using REGEX   #
#####################################################

# Import calls for this application
import re  
import io
import os
import sys


# Set our working directory
if (len(sys.argv)) >= 2:
    if '\"' in (sys.argv[1]): os.chdir(sys.argv[1].replace('\"', ' '))
    else: os.chdir(sys.argv[1])

# Open the RC file for the project output and store it.
fulcrum_dll_rc_file = open("res\\fulcrum_shim.rc", "r")
rc_file_contents = fulcrum_dll_rc_file.read()
fulcrum_dll_rc_file.close()

# Now regex out the current version information
print ("\n+---------------------------------------------------------------------+")
print ("|             FULCRUM DLL VERSION TICKING SCRIPT - v0.1               |")
print ("|      USE THIS SCRIPT TO TICK THE BUILD VERSION ON A C++ PROJECT     |")
print ("+---------------------------------------------------------------------+\n")

# Find our matches here.
print ("---------------------------------------------------------------------")
print ("FINDING CURRENT REGEX MATCHES IN OUR RC FILE NOW...")
found_matches = re.findall(r"(FILE|File|PRODUCT|Product)(Version|VERSION)(\s|\",\s\")((\d+(\.|,))+\d+)", rc_file_contents)

# Loop each one and increase the last value in each set now.
print ("PRINTING OLD AND NEW FILE/PRODUCT VERSION STRINGS NOW...")
print ("----------------------------------------------------------------------\n")
for match_value in found_matches:
    current_item_index = str(found_matches.index(match_value) + 1)
    if "," in match_value[3]: 
        # For the , split values
        original_version_string = match_value[0] + match_value[1] + " " + match_value[3]

        # Now update values
        last_version_split = match_value[3].split(",")
        ticked_version_number = int(last_version_split[-1]) + 1
        last_version_split[-1] = str(ticked_version_number)
        new_version_value =  ",".join(last_version_split)
        new_version_string = match_value[0] + match_value[1] + " " + new_version_value

        # Print comparison values now.
        print ("--> MATCH NUMBER " + current_item_index)
        print ("    \\__ OLD VERSION: " + original_version_string)
        print ("    \\__ NEW VERSION: " + new_version_string)

        # Run the replace command here based on type in the file.
        if "FILE" in match_value[0]: rc_file_contents = re.sub(r"((FILEVERSION\s)(\d+(\.|,))+\d+)", new_version_string, rc_file_contents)
        if "PRODUCT" in match_value[0]: rc_file_contents = re.sub(r"((PRODUCTVERSION\s)(\d+(\.|,))+\d+)", new_version_string, rc_file_contents)

        # Print replacement done OK
        print ("    \\__ REPLACED NEW VERSION STRING INTO THE FILE CONTENT OK!")
        print ("")

    else: 
        # For the . split values
        original_version_string = "\"" + match_value[0] + match_value[1] + "\", \"" + match_value[3] + "\""

        # Now update values
        last_version_split = match_value[3].split(".")
        ticked_version_number = int(last_version_split[-1]) + 1
        last_version_split[-1] = str(ticked_version_number)
        new_version_value = ".".join(last_version_split)
        new_version_string = "\"" + match_value[0] + match_value[1] + "\", \"" + new_version_value + "\""

        # Print comparison values now.
        print ("--> MATCH NUMBER " + current_item_index)
        print ("    \\__ OLD VERSION: " + original_version_string)
        print ("    \\__ NEW VERSION: " + new_version_string)

        # Run the replace command here based on type in the file.
        if "File" in match_value[0]: rc_file_contents = re.sub(r"(((\"FileVersion\",)\s\")(\d+(\.|,))+\d+\")", new_version_string, rc_file_contents)
        if "Product" in match_value[0]: rc_file_contents = re.sub(r"(((\"ProductVersion\",)\s\")(\d+(\.|,))+\d+\")", new_version_string, rc_file_contents)

        # Print replacement done OK
        print ("    \\__ REPLACED NEW VERSION STRING INTO THE FILE CONTENT OK!")
        print ("")

# Print file content to store out here.
print ("----------------------------------------------------------------------")
print ("CONTENT FOR OUR NEW RC FILE IS SHOWN BELOW")
print ("VERSION FOR ALL PRODUCT AND FILE ENTRIES SHOULD BE TICKED BY ONE\n")
version_infos_only = "VS_VERSION_INFO" + rc_file_contents.split('VS_VERSION_INFO')[1].split('END\n\n')[0] + "END\n"
version_infos_only = "\n".join(["     " + strPart for strPart in version_infos_only.split("\n")])
print (version_infos_only)

# Now print the contents of the file out into the rc file for the build output.
print ("----------------------------------------------------------------------")
print ("WRITING NEW RC FILE CONTENTS OUT NOW...")
with open(".\\res\\fulcrum_shim.rc", "r+") as fulcrum_dll_rc_file:
    fulcrum_dll_rc_file.seek(0)
    fulcrum_dll_rc_file.write(rc_file_contents)
    fulcrum_dll_rc_file.truncate()
print ("WROTE OUT RC FILE CONTENT OK! VERSION HAS BEEN SET CORRECTLY!")
print ("----------------------------------------------------------------------\n")