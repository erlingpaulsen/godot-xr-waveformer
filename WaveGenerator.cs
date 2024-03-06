using Godot;
using System;

public partial class WaveGenerator : Node
{
	public enum Hand
	{
		Left = -1,
		Right = 1
	}
	[Export]
	public Hand SelectedHand { get; set; } = Hand.Left;
	public enum Waveform
	{
		Sine,
		Triangle,
		Square,
		Sawtooth,
		Eventooth,
		Evenangle,
		Parabolic
	}
	[Export]
	public Waveform PrimaryWaveform { get; set; } = Waveform.Sawtooth;
	public Waveform SecondaryWaveform { get; set; } = Waveform.Sawtooth;
	public float WaveFrequency = 220.0f;
	public float SampleFrequency;
	public float PrimaryPhase;
	public float SecondaryPhase;
	public float Increment;
	[Export(PropertyHint.Range, "1,11,1,or_greater,or_less")]
	public int SubOscillatorIndex = 6;

	[Export]
	public AudioStreamPlayer3D AudioStreamPlayer;
	public AudioStreamGeneratorPlayback Playback;
	public AudioStreamGenerator Generator;
	[Export]
	public XRController3D Controller;
	public float ControllerX;
	public float ControllerY;
	public float PitchSensitivity = 600.0f;
	public float FilterSensitivity = 20000.0f;
	public AudioEffectLowPassFilter LowpassFilter;
	public int AudioBusIdx;

	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Generator = AudioStreamPlayer.Stream as AudioStreamGenerator;
		SampleFrequency = Generator.MixRate;
		Increment = WaveFrequency / SampleFrequency;
		ControllerX = Controller.Position.X;
		ControllerY = Controller.Position.Y;
		AudioBusIdx = AudioServer.GetBusIndex(AudioStreamPlayer.Name);
		LowpassFilter = AudioServer.GetBusEffect(AudioBusIdx, 0) as AudioEffectLowPassFilter;
		AudioStreamPlayer.Play();
		Playback = AudioStreamPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
		Controller.ButtonPressed += OnButtonPressed;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Controller.IsButtonPressed("trigger"))
		{
			FillBuffer();
		}
		//WaveFrequency = Mathf.Clamp(WaveFrequency + PitchSensitivity * (Controller.Position.Y - ControllerY), 20.0f, 10000.0f);
		WaveFrequency = Mathf.Clamp(PitchSensitivity * (Controller.Position.Y - 1.0f) + 220.0f, 20.0f, 10000.0f);
		//LowpassFilter.CutoffHz = Mathf.Clamp(LowpassFilter.CutoffHz + FilterSensitivity * (float)SelectedHand * (Controller.Position.X - ControllerX), 100.0f, 20000.0f);
		LowpassFilter.CutoffHz = Mathf.Clamp((float)SelectedHand * FilterSensitivity * (Controller.Position.X - (float)SelectedHand * 0.07f), 20.0f, 20000.0f);
		Increment = WaveFrequency / SampleFrequency;
		ControllerX = Controller.Position.X;
		ControllerY = Controller.Position.Y;
	}


    private void FillBuffer()
	{
		// Create Vector2 array to hold the audio data frames
		int numFrames = Playback.GetFramesAvailable();
		Vector2[] inputBuffer = new Vector2[numFrames];
		for (int i=0; i < numFrames; i++)
		{
			float primarySignal = GenerateWave(PrimaryWaveform, PrimaryPhase);
			float secondarySignal = GenerateWave(SecondaryWaveform, SecondaryPhase);
			float mixedSignal = MixSignals(primarySignal, secondarySignal, Controller.Position.Z + 0.7f);
			inputBuffer[i] = new Vector2(mixedSignal, mixedSignal);
			PrimaryPhase = (float)Mathf.PosMod(PrimaryPhase + Increment, 1.0);
			SecondaryPhase = (float)Mathf.PosMod(SecondaryPhase + SubOscillatorIndex * Increment / 12, 1.0);
		}
		// Push buffer to the audio stream
		if (Playback.CanPushBuffer(numFrames)) {
			Playback.PushBuffer(inputBuffer);
		}

	}

	private static float GenerateWave(Waveform type, float phase)
	{
		float output = 0;
		int n = 8;
		if (type == Waveform.Sine)
		{
			output = Mathf.Sin(phase * Mathf.Tau);
		}
		else if (type == Waveform.Triangle)
		{
			output = 4 * Mathf.Abs(phase - (float)Mathf.Floor(phase + 0.5)) - 1;
			// output = HarmonicSeries(phase, n, Harmonics.Odd, HarmonicStrength.Squared, HarmonicBasis.Cos);
		}
		else if (type == Waveform.Square)
		{
			output = Mathf.Sign(Mathf.Sin(phase * Mathf.Tau));
			// output = HarmonicSeries(phase, n, Harmonics.Odd, HarmonicStrength.Linear, HarmonicBasis.Sin);
		}
		else if (type == Waveform.Sawtooth)
		{
			output = 2 * phase - 1;
			// output = HarmonicSeries(phase, n, Harmonics.All, HarmonicStrength.Linear, HarmonicBasis.Sin);
		}
		else if (type == Waveform.Eventooth)
		{
			output = HarmonicSeries(phase, n, Harmonics.Even, HarmonicStrength.Linear, HarmonicBasis.Sin);
		}
		else if (type == Waveform.Evenangle)
		{
			output = HarmonicSeries(phase, n, Harmonics.Even, HarmonicStrength.Squared, HarmonicBasis.Cos);
		}
		else if (type == Waveform.Parabolic)
		{
			output = HarmonicSeries(phase, n, Harmonics.All, HarmonicStrength.Squared, HarmonicBasis.Cos);
		}
		return output;
	}

	private static float MixSignals(float signalX, float signalY, float mixFactor)
	{
		return (Mathf.Clamp(mixFactor, 0, 1) * signalX) + ((1 - Mathf.Clamp(mixFactor, 0, 1)) * signalY);
	}

	private static Waveform RotateWaveform(Waveform waveform)
	{
		return (Waveform)Mathf.PosMod((int)waveform + 1, (int)Waveform.Sawtooth);
	}

	private static int RotateSubOscillatorIdx(int idx)
	{
		return Mathf.PosMod(idx, 10) + 1;
	}

	private void OnButtonPressed(string keyName)
	{
		//GD.Print(keyName, " button pressed");
		if (keyName == "ax_button")
		{
			if (Controller.IsButtonPressed("grip"))
			{
				SecondaryWaveform = RotateWaveform(SecondaryWaveform);
				GD.Print(SelectedHand.ToString(), " hand: Secondary waveform changed to ", SecondaryWaveform.ToString());
			}
			else
			{
				PrimaryWaveform = RotateWaveform(PrimaryWaveform);
				GD.Print(SelectedHand.ToString(), " hand: Primary waveform changed to ", PrimaryWaveform.ToString());
			}
		}
		else if (keyName == "by_button")
		{
			 SubOscillatorIndex = RotateSubOscillatorIdx(SubOscillatorIndex);
			 GD.Print(SelectedHand.ToString(), " hand: Sub oscillator index changed to ", SubOscillatorIndex);
		}
	}

	private enum Harmonics
	{
		Odd,
		Even,
		All
	}

	private enum HarmonicStrength
	{
		Linear,
		Squared
	}

	private enum HarmonicBasis
	{
		Cos,
		Sin
	}

	private static float HarmonicSeries(float phase, int n, Harmonics harmonics, HarmonicStrength harmonicStrength, HarmonicBasis harmonicBasis)
	{
		// float output = 0.0f;
		// int start = 1;
		// int step = 1;

		// if (harmonics == Harmonics.Odd)
		// {
		// 	start = 3;
		// 	step = 2;
		// }
		// else if (harmonics == Harmonics.Even)
		// {
		// 	start = 2;
		// 	step = 2;
		// }
		// else if (harmonics == Harmonics.All)
		// {
		// 	start = 2;
		// 	step = 1;
		// }

		// if (harmonicBasis == HarmonicBasis.Cos)
		// {
		// 	output += Mathf.Cos(phase * Mathf.Tau);
		// }
		// else if (harmonicBasis == HarmonicBasis.Sin)
		// {
		// 	output += Mathf.Sin(phase * Mathf.Tau);
		// }
		// for (int i = start; i < n * step; i += step)
		// {
		// 	int k = 1;
		// 	if (harmonicStrength == HarmonicStrength.Linear)
		// 	{
		// 		k = i;
		// 	}
		// 	else if (harmonicStrength == HarmonicStrength.Squared)
		// 	{
		// 		k = i*i;
		// 	}
		// 	if (harmonicBasis == HarmonicBasis.Cos)
		// 	{
		// 		output += 1.0f / k * Mathf.Cos(k * phase * Mathf.Tau);
		// 	}
		// 	else if (harmonicBasis == HarmonicBasis.Sin)
		// 	{
		// 		output += 1.0f / k * Mathf.Sin(k * phase * Mathf.Tau);
		// 	}
		// }

		float output = 0.0f;
		int start = 0;

		if (harmonics == Harmonics.Even || harmonics == Harmonics.All)
		{
			start = 1;
		}
		// Creating n components of the harmonic series
		for (int i = start; i < n + start; i++)
		{
			int k = 1;
			if (harmonics == Harmonics.Odd)
			{
				k = 2 * i + 1;
			}
			else if (harmonics == Harmonics.Even)
			{
				k = 2 * i;
			}
			else if (harmonics == Harmonics.All)
			{
				k = i;
			}
			int p = 1;
			if (harmonicStrength == HarmonicStrength.Squared)
			{
				p = 2;
			}
			//output += Mathf.Pow(-1, i) * 1 / Mathf.Pow(k, p) * Mathf.Sin(k * phase * Mathf.Tau);
			output += 1 / Mathf.Pow(k, p) * Mathf.Cos(k * phase * Mathf.Tau);
		}
		return output;
	}
}
