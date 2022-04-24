using System.Speech.Synthesis;

namespace SpeechSynthesizerNS
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.GetLength(0) == 0 || string.IsNullOrWhiteSpace(args[0]))
				return;
			
			string sentence = "";
			foreach (var arg in args)
				sentence += arg;

            SpeechSynthesizer _synthesizer = new SpeechSynthesizer();
            _synthesizer.Speak(sentence);
		}
	}
}
