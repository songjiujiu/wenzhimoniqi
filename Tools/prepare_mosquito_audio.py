"""Prepare a loop from MarianaRA's real mosquito recording (CC0, Freesound 477794).
Requires ffmpeg on PATH and numpy. No synthesized oscillator is mixed in.
"""
from pathlib import Path
import json, subprocess, wave
import numpy as np
ROOT=Path(__file__).resolve().parent.parent
SOURCE=ROOT/'SourceArt/Audio/MarianaRA-MosquitoBuzzing-477794.mp3'
CACHE=ROOT/'.tools-cache'
RATE=44100
def decode(path, filters=None):
    args=['ffmpeg','-hide_banner','-loglevel','error','-i',str(path),'-ac','1','-ar',str(RATE)]
    if filters: args+=['-af',filters]
    return np.frombuffer(subprocess.check_output(args+['-f','f32le','pipe:1']),dtype='<f4').copy()
raw=decode(SOURCE)
# Penalize bag crackles (broadband transients), silence and rumble. Choose an
# eight-second excerpt with sustained wingbeat harmonics, preserving its timing.
size=4096; freq=np.fft.rfftfreq(size,1/RATE); rows=[]
for second in range(2,int(len(raw)/RATE)-10):
    clip=raw[second*RATE:(second+8)*RATE]
    windows=np.lib.stride_tricks.sliding_window_view(clip,size)[::size]
    power=abs(np.fft.rfft(windows*np.hanning(size),axis=1))**2
    buzz=power[:,(freq>=250)&(freq<2400)].sum(axis=1)
    crackle=power[:,(freq>=4000)&(freq<15000)].sum(axis=1)
    rumble=power[:,freq<160].sum(axis=1)
    rms=np.sqrt(np.mean(windows**2,axis=1))
    score=np.median(buzz/(crackle*3+rumble*.5+1e-8))*np.percentile(rms,20)/(np.percentile(rms,95)+1e-8)
    crest=np.max(abs(clip))/(np.sqrt(np.mean(clip**2))+1e-8)
    score/=crest**2
    rows.append((float(score),second))
_,start=max(rows)
filtered=decode(SOURCE,'highpass=f=180,lowpass=f=3500,afftdn=nf=-55:nr=6')
segment=filtered[start*RATE:(start+8)*RATE].astype(np.float64)
segment-=segment.mean()
# Rotate with an overlap at the join: the end transitions continuously into
# the beginning, avoiding a fade-to-silence every time AudioSource loops.
overlap=int(.16*RATE); ramp=np.linspace(0,1,overlap,endpoint=False)
cross=segment[-overlap:]*(1-ramp)+segment[:overlap]*ramp
loop=np.concatenate((cross,segment[overlap:-overlap]))
loop*=.72/max(np.max(np.abs(loop)),1e-8)
destination=ROOT/'Assets/Game/Resources/Audio/MosquitoBuzzLoop.wav'
destination.parent.mkdir(parents=True,exist_ok=True)
with wave.open(str(destination),'wb') as output:
    output.setnchannels(1); output.setsampwidth(2); output.setframerate(RATE)
    output.writeframes((np.clip(loop,-1,1)*32767).astype('<i2').tobytes())
report={'source':'https://freesound.org/people/MarianaRA/sounds/477794/',
        'author':'MarianaRA','license':'CC0 1.0',
        'download':'https://cdn.freesound.org/previews/477/477794_3472612-hq.mp3',
        'source_format':'Public high-quality MP3 preview of the original recording',
        'excerpt_start_seconds':start,'excerpt_seconds':8,'overlap_seconds':.16,
        'output_seconds':len(loop)/RATE,'sample_rate':RATE,'channels':1,
        'rms_dbfs':float(20*np.log10(np.sqrt(np.mean(loop**2)))),
        'peak':float(np.max(abs(loop))), 'seam_delta':float(abs(loop[-1]-loop[0])),
        'processing':'Mono, 180 Hz high-pass, 3500 Hz low-pass, light FFT denoise, DC removal, loop crossfade, peak -2.85 dBFS. No pitch shift or synthetic tone.'}
(ROOT/'SourceArt/Audio/MosquitoBuzzLoop-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
