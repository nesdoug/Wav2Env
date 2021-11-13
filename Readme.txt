Wav2Env
Converts a WAV file to Famitracker Volume Envelope

.NET, should run on non-windows systems with MONO

-------
 Usage
-------
Open Wav2Env
Load a WAV file
Copy the text output
Open Famitracker
Open a New Instrument
(highlight Volume)
Paste to the textbox, press enter

---------
 Options
---------
Change the System (NTSC/PAL)
Change the Normalize Volume

-------------
 Limitations
-------------
It only outputs max 64 envelope values.
That is 1.067 seconds, NTSC
That is 1.28 seconds, PAL
If you need more than than, you will have
to split the WAV into 1 second chunks and
process each one separately.

I tried to make this app flexible enough
to handle standard WAV files, but
maybe there's some variety of WAV that
won't work. If that happens, open that 
file in Audacity and export a new WAV 
file. It should work then.

Technical details of WAVs that work...
Min sample rate = 8000 Hz
Max sample rate = 48000 Hz
# of channels = 1 or 2 
(ie. use MONO or STEREO files)
Needs to be Signed PCM format,
16 bit or 24 bit formats

------
 Tips
------
You will probably get best results if you
use some other app (like Audacity) to
trim any silence off the beginning, and
boost the volume to 100%, export a WAV
before loading it in Wav2Env

You may have to write a zero to the end
of the envelope, if you get a final value
not zero.



