# Polling and logging usage for Dynamics GP

## Setting up
* Edit UsagePollerSettings.json.template per your requirements
* Rename to UsagePollerSettings.json.  This file needs to be in the same directory as the executable.
* In ActivityChecker.Checker.EmailLongUsers, adjust email address domain.
* Build executable.  Designed to run as a Windows Service, so you can use the installer if you wish.  Otherwise, just run the binary.
* Install _nb_GetActivity_Function() in the default database.
* Create table activityTrackingLog.

## Things to watch out for
* Current assumption is that email addresses are `gpusername@hardcodeddomain`.  Handle as needed.
* If there is an exception, the service should log the error and then throw a new, blank error to the OS and stop.  If you're running this as a Windows service, let the OS handle restarts. 
