import whisper

def transcribe_audio(input_file, output_file):
    model = whisper.load_model("base")
    result = model.transcribe(input_file)
    with open(output_file, "w") as f:
        f.write(result["srt"])

if __name__ == "__main__":
    import sys
    transcribe_audio(sys.argv[1], sys.argv[2])
