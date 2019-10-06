function sleep(ms: number) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

let context = new AudioContext();

enum fq {
	f2 = 87.31,
	g2 = 98.00,
	a2 = 110.00,
	g3 = 196.00,
	a3 = 220.00,
	c4 = 261.63,
	d4 = 293.66,
	e4 = 329.63,
	f4 = 349.23,
	g4 = 392.00,
	a4 = 440,
	b4 = 493.88,
	c5 = 523.25,
	d5 = 587.33,
	e5 = 659.25,
	f5 = 698.46,
	g5 = 783.99,
	a5 = 880,
	c6 = 1046.50
}

class Reverb {
	context: AudioContext;
	seconds: number;
	decay: number;

	public node: ConvolverNode;

	constructor(context: AudioContext, seconds: number, decay: number) {
		this.node = context.createConvolver();
		this.seconds = seconds;
		this.decay = decay;
		this.context = context;
		this.create();
	}

	connect(node: AudioNode) {
		this.node.connect(node);
	}

	create() {
		let rate = this.context.sampleRate
        , length = rate * this.seconds
        , decay = this.decay
        , impulse = this.context.createBuffer(2, length, rate)
        , impulseL = impulse.getChannelData(0)
        , impulseR = impulse.getChannelData(1)
        , n, i;

      for (i = 0; i < length; i++) {
        n = false ? length - i : i;
        impulseL[i] = (Math.random() * 2 - 1) * Math.pow(1 - n / length, decay);
        impulseR[i] = (Math.random() * 2 - 1) * Math.pow(1 - n / length, decay);
	  }
	  
	  this.node.buffer = impulse;
	}
}

let samples: AudioBuffer[] = [];

enum sample {
	kick,
	snare,
	hihat
}

class Utils {
	public static async loadSample(sample: string) {
		let done = false;
		let r = new XMLHttpRequest();
		r.open('GET', sample, true);
		r.responseType = 'arraybuffer';
		r.onload = () => {
			done = true;
			let audioData = r.response;
			context.decodeAudioData(audioData, (buffer) => {
				samples.push(buffer);
			},(err: any) => alert(err))			
		};
		r.send();
		while(!done) {
			await sleep(100);
		}
	}
}

class Sound {
	buffer: AudioBufferSourceNode;
	oscillator: OscillatorNode;
	gainNode: GainNode;

	sample: sample;

	createOscillator(type: OscillatorType, volume: number) {
		this.oscillator = context.createOscillator();

		this.oscillator.type = type;
		this.oscillator.frequency.setValueAtTime(440, context.currentTime);
		this.gainNode = context.createGain();
		this.gainNode.connect(context.destination);
		this.gainNode.gain.setValueAtTime(volume, context.currentTime);
		this.oscillator.connect(this.gainNode);
		
	}

	async loadSample(sample: sample) {
		this.sample = sample;
	}

	async createFromSample(sample: sample) {
		this.gainNode = context.createGain();
		this.gainNode.connect(context.destination);
		this.gainNode.gain.setValueAtTime(0.1, context.currentTime);

		this.buffer = context.createBufferSource();
		this.buffer.buffer = samples[sample];
		this.buffer.connect(this.gainNode);
	}

	reverb(seconds: number, decay: number) {
		let reverb = new Reverb(context, seconds, decay);
		this.gainNode.connect(reverb.node);
		reverb.node.connect(context.destination);
	}

	frequency(freq: fq, when?: number) {
		if(this.oscillator != null)
			this.oscillator.frequency.setValueAtTime(freq, when != null ? context.currentTime+when : context.currentTime);
	}

	vol(vol: number, when?: number) {
		this.gainNode.gain.setValueAtTime(vol, when != null ? context.currentTime+when : context.currentTime);
	}

	play(when?: number) {
		if(this.oscillator != null)
			this.oscillator.start(when);
		if(this.sample != null) {
			this.createFromSample(this.sample);
			this.reverb(0.5,1);
			this.buffer.start(when);
		}
			
	}
}

class ArpChord {

	constructor(tones: fq[],) {

	}


}

async function main() {
	let sq = new Sound()
	sq.createOscillator('triangle', 0.1);
	sq.reverb(3, 2);
	//sq.play();

	let b = new Sound();
	b.createOscillator('sine', 0.1);
	b.reverb(2,1);
	//b.play();
	b.frequency(fq.a2);

	await Utils.loadSample('itkick.wav');
	await Utils.loadSample('itsnare.wav');
	await Utils.loadSample('ithat.wav');
	let k = new Sound();
	k.loadSample(sample.kick);
	let s = new Sound();
	s.loadSample(sample.snare);
	let hh = new Sound();
	hh.loadSample(sample.hihat);

	sq.play();
	b.play();
	//k.frequency(fq.c4);
	let mel = new Sound();
	mel.createOscillator('square', 0.1);
	mel.reverb(3, 2);
	mel.vol(0);
	mel.play();

	let q, w, e, r, t, y, u = false;
	window.onkeydown = (ev: KeyboardEvent) => {
		if (ev.key == 'q') {
			q = true;
			mel.vol(0.05);
			mel.frequency(fq.c4, 0);
		}
		if (ev.key == 'w') { 
			w = true;
			mel.vol(0.05);
			mel.frequency(fq.d4, 0);
		}
		if (ev.key == 'e') {
			e = true; 
			mel.vol(0.05);
			mel.frequency(fq.e4, 0);
		}
		if (ev.key == 'r') {
			r = true; 
			mel.vol(0.05);
			mel.frequency(fq.f4, 0);
		}
		if (ev.key == 't') {
			t = true; 
			mel.vol(0.05);
			mel.frequency(fq.g4, 0);
		}
		if (ev.key == 'y') {
			y = true; 
			mel.vol(0.05);
			mel.frequency(fq.a4, 0);
		}
		if (ev.key == 'u') {
			u = true; 
			mel.vol(0.05);
			mel.frequency(fq.b4, 0);
		}
	};

	window.onkeyup = (ev: KeyboardEvent) => {
		if(ev.key == 'q') q = false;
		if(ev.key == 'w') w = false;
		if(ev.key == 'e') e = false;
		if(ev.key == 'r') r = false;
		if(ev.key == 't') t = false;
		if(ev.key == 'y') y = false;
		if(ev.key == 'u') u = false;

		if(!q && !w && !e && !r && !t && !y && !u)
			mel.vol(0);
	}


	let x = 0;
	while(true) {
		x++;
		for(let i=1;i<9;i++) {
			if (x > 1) {
				k.play();
				if (i == 4 || i == 8) s.play();
			}

			b.frequency(fq.a2);
			sq.frequency(fq.e5);
			await sleep(150);
			sq.frequency(fq.c5);
			await sleep(150);
			b.frequency(fq.a3);
			sq.frequency(fq.a4);
			await sleep(150)
			sq.frequency(i == 7 ? fq.g4 : fq.e4);
			await sleep(150)
		}
		//mel.vol(0.05);
		//mel.frequency(fq.b4, context.currentTime+0);
		//mel.frequency(fq.d5, context.currentTime+2);
		//mel.frequency(fq.g4, context.currentTime+4);
		for(let i=0;i<8;i++) {
			if (x > 1) {
				k.play();
				if (i == 3 || i == 7) s.play();
			}
			b.frequency(fq.g2);
			sq.frequency(fq.d5);
			await sleep(150)
			sq.frequency(fq.b4);
			await sleep(150)
			b.frequency(fq.g3);
			sq.frequency(fq.g4);
			await sleep(150)
			sq.frequency(i == 8 ? fq.f4 : fq.d4);
			await sleep(150)
		}
		b.frequency(fq.f2);
		for(let i=0;i<4;i++) {
			if (x > 1) {
				k.play();
				if (i == 3) s.play();
			}
			sq.frequency(fq.c5);
			await sleep(150)
			sq.frequency(fq.a4);
			await sleep(150)
			sq.frequency(fq.f4);
			await sleep(150)
			sq.frequency(i == 3 ? fq.e4 : fq.c4);
			await sleep(150)
		}

		//await sleep(25)
		//sq.frequency.setValueAtTime(196.00, context.currentTime);
		//await sleep(25)
		//sq.frequency.setValueAtTime(659.25, context.currentTime);
		//await sleep(25)
	}
}

main();