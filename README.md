# FlickrSuperSync
A simple tool to download all your photos from Flickr

# Introduction
After search for a while for a tool that helps me to download all my photos from Flickr, and also allow me to resume download in case of some failings, I decided to create my own project.

It's very simple, but it works very well and was managed to accomplish its main goal.

# Basic usage
FlickrSuperSyncConsole.exe [Target Dir]

Exemple:
FlickrSuperSyncConsole.exe "C:\Flickr Photos"

It will download all your photos on target dir. If the process stops in the midle, just run that command again.

If you need to start over, pass the reset argument, as follows:

FlickrSuperSyncConsole.exe "C:\Flickr Photos" "" -R

The second parameter is the album name, but that feature is not fully implemented yet.
