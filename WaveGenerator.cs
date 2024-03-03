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
		Sawtooth
	}
	[Export]
	public Waveform CurrentWaveform { get; set; } = Waveform.Sawtooth;
	public float WaveFrequency = 220.0f;
	public float SampleFrequency;
	public float Phase;
	public float PhaseSub;
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
		WaveFrequency = Mathf.Clamp(WaveFrequency + PitchSensitivity * (Controller.Position.Y - ControllerY), 20.0f, 10000.0f);
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
			float inputSignal = GenerateWave(CurrentWaveform, Phase);
			float inputSignalSub = GenerateWave(CurrentWaveform, PhaseSub);
			float inputSignalMixed = MixSignals(inputSignal, inputSignalSub, Controller.Position.Z + 0.7f);
			inputBuffer[i] = new Vector2(inputSignalMixed, inputSignalMixed);
			Phase = (float)Mathf.PosMod(Phase + Increment, 1.0);
			PhaseSub = (float)Mathf.PosMod(PhaseSub + SubOscillatorIndex * Increment / 12, 1.0);
		}
		// Push buffer to the audio stream
		if (Playback.CanPushBuffer(numFrames)) {
			Playback.PushBuffer(inputBuffer);
		}

	}

	private static float GenerateWave(Waveform type, float phase)
	{
		float output = 0;
		if (type == Waveform.Sine)
		{
			output = Mathf.Sin(phase * Mathf.Tau);
		}
		else if (type == Waveform.Triangle)
		{
			output = 4 * Mathf.Abs(phase - (float)Mathf.Floor(phase + 0.5)) - 1;
		}
		else if (type == Waveform.Square)
		{
			output = Mathf.Sign(Mathf.Sin(phase * Mathf.Tau));
		}
		else if (type == Waveform.Sawtooth)
		{
			output = 2 * phase - 1;
		}
		return output;
	}

	private static float MixSignals(float signalX, float signalY, float mixFactor)
	{
		return (Mathf.Clamp(mixFactor, 0, 1) * signalX) + ((1 - Mathf.Clamp(mixFactor, 0, 1)) * signalY);
	}

	private void RotateWaveform()
	{
		CurrentWaveform = (Waveform)Mathf.PosMod((int)CurrentWaveform + 1, (int)Waveform.Sawtooth);
	}

	private void RotateSubOscillatorIdx()
	{
		SubOscillatorIndex = Mathf.PosMod(SubOscillatorIndex + 1, 10) + 1;
	}

	private void OnButtonPressed(string keyName)
	{
		//GD.Print(keyName, " button pressed");
		if (keyName == "ax_button")
		{
			 RotateWaveform();
		}
		else if (keyName == "by_button")
		{
			 RotateSubOscillatorIdx();
		}
	}
}
