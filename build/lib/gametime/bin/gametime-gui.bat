@echo off
rem Prepares and starts the GameTime graphical user interface.

rem See the LICENSE file, located in the root directory of
rem the source distribution and
rem at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
rem for details on the GameTime license and authors.


rem Run the Microsoft Phoenix batch script.
cd "C:\Program Files (x86)\Phoenix SDK June 2008\bin\debug"
call phxvars32d.bat x86
echo.

rem Add the path to the `bin` subdirectory, in
rem the directory for the installed Python package, to PATH.
set PATH="C:\Python27\Lib\site-packages\gametime-1.5-py2.7.egg\gametime\bin";%PATH%

rem Use the Cygwin bash shell to run the Python script
rem that displays the GameTime GUI. We use this roundabout
rem mechanism to use the same PATH as the GameTime CLI.
chdir "C:\cygwin64\bin"
bash --login -c "/cygdrive/c/Python27/python C:/Python27/Lib/site-packages/gametime-1.5-py2.7.egg/gametime/gui/gui.py"
