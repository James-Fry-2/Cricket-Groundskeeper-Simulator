Cricket Groundsman: playtest build for macOS
=============================================

Thanks for testing. Guide.md, in this folder, explains the game in a few pages.

Starting it
-----------
1. Unzip this folder somewhere, for example your Desktop. Keep the content folder next to
   CricketGroundsman: the game reads it.
2. Open Terminal (Applications > Utilities > Terminal), type "cd " (with a space), drag this
   folder onto the Terminal window, and press Return.
3. Type ./CricketGroundsman and press Return.

The first time, macOS will say it can't check the app for malicious software, because this
test build isn't signed. To allow it:
- open System Settings > Privacy & Security, scroll down, and click "Open Anyway" next to
  CricketGroundsman; then run ./CricketGroundsman again and choose Open.
- Or, in Terminal in this folder, run:  xattr -d com.apple.quarantine CricketGroundsman

Make the Terminal window at least 80 characters wide. A larger window is easier on the eyes.

Stopping and carrying on
------------------------
Type q to quit at any time. The game saves after every turn; next time it asks whether to carry
on where you left off.

Sending it back
---------------
Your seasons are in Documents/Cricket Groundsman, one folder per season. When you've finished
(or given up), zip that season's folder and send it with the questionnaire. Nothing is sent
from your Mac on its own. If you jot notes while playing, save them as notes.txt in the
season's folder.
