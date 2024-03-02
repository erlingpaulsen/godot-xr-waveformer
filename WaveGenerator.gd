extends Node

var playback: AudioStreamGeneratorPlayback # Will hold the AudioStreamGeneratorPlayback.
@export var audio_stream_player: AudioStreamPlayer3D
@export var right_hand: XRController3D

enum Waveform {Sine, Triangle, Square, Sawtooth}
@export var waveform: Waveform
@onready var sample_freq: int = audio_stream_player.stream.mix_rate
var wave_freq := 220.0 # The frequency of the sound wave.
var phase := 0.0
@onready var increment := wave_freq / sample_freq
@onready var right_hand_y := right_hand.position.y
@onready var right_hand_x := right_hand.position.x
var pitch_sensitivity := 600.0
var filter_sensitivity := 15000.0
var lowpass_filter: AudioEffectLowPassFilter

#var dn_1 = 0.0
#var cutoff_freq = 220.0

func _ready() -> void:
	lowpass_filter = AudioServer.get_bus_effect(0, 0)
	#print(lowpass_filter.cutoff_hz)
	audio_stream_player.play()
	#audio_stream_player.stream_paused = true
	playback = audio_stream_player.get_stream_playback()
	#fill_buffer()

func _process(delta: float) -> void:
	if right_hand.is_button_pressed("trigger"):
		#audio_stream_player.stream_paused = false
		fill_buffer()
	#else:
		#audio_stream_player.stream_paused = true
		#playback.clear_buffer()
	wave_freq = clampf(wave_freq + pitch_sensitivity * (right_hand.position.y - right_hand_y), 20, 10000)
	lowpass_filter.cutoff_hz = clampf(lowpass_filter.cutoff_hz + filter_sensitivity * (right_hand.position.x - right_hand_x), 100, 20000)
	increment = wave_freq / sample_freq
	right_hand_y = right_hand.position.y
	right_hand_x = right_hand.position.x

func fill_buffer() -> void:
	#var a1 = a1_coefficient(cutoff_freq, sample_freq)
	for i in range(playback.get_frames_available()):
		var input_signal: float
		if waveform == Waveform.Sine:
			input_signal = sin(phase * TAU)
		elif waveform == Waveform.Triangle:
			input_signal = 4 * abs(phase - floorf(phase + 0.5)) - 1
		elif waveform == Waveform.Square:
			input_signal = sign(sin(phase * TAU))
		elif waveform == Waveform.Sawtooth:
			input_signal = 2 * phase - 1
		
		#var allpass_output = a1 * input_signal + dn_1
		#var filter_output = 0.5 * (input_signal + allpass_output)
		# Stereo channel vec2
		playback.push_frame(Vector2.ONE * input_signal)
		phase = fmod(phase + increment, 1.0)
		#dn_1 = input_signal - a1 * allpass_output

func a1_coefficient(cutoff_freq, sample_freq) -> float:
	var tan = tan(PI * cutoff_freq / sample_freq)
	return (tan - 1) / (tan + 1)
