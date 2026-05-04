#!/bin/bash

# Build the project
dotnet build

# Check if build was successful
if [ $? -eq 0 ]; then
    echo "Build successful. Copying files to workshop directory..."
    # Copy files from build directory to the specified workshop path
    cp -r build/* /d/SteamLibrary/steamapps/workshop/content/2059170/3705314255/
    echo "Files copied successfully."
else
    echo "Build failed. Aborting copy operation."
    exit 1
fi


