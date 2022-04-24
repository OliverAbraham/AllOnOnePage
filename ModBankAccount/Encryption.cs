using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Abraham.Security
{
    /// <summary>
    /// Kennzeichnet eine unbrauchbare Kennwortl�nge, um etwas zu verschl�sseln
    /// </summary>
    public class IllegalPasswordLengthException : Exception
    {
        /// <summary>
        /// Kennzeichnet eine unbrauchbare Kennwortl�nge, um etwas zu verschl�sseln
        /// </summary>
        public IllegalPasswordLengthException() 
        { 
        }

        /// <summary>
        /// Kennzeichnet eine unbrauchbare Kennwortl�nge, um etwas zu verschl�sseln
        /// </summary>
        public IllegalPasswordLengthException(string message) 
        { 
        }
    }


    /// <summary>
    /// Verschl�sselt und entschl�sselt texte mit dem AES-Algorithmus
    /// </summary>
    /// 
    /// <remarks>
    ///-------------------------------------------------------------------------------------------------
    ///
    ///                                 Oliver Abraham
    ///                              www.oliver-abraham.de
    ///                              mail@oliver-abraham.de
    ///
    ///              Klasse f�r die Verschl�sselung und Entschl�sselung von Daten
    ///
    ///-------------------------------------------------------------------------------------------------
    ///
    /// Literatur:
    /// http://www.codeproject.com/KB/security/SimpleEncryption.aspx
    ///
    ///
    /// Wichtiger Hinweis:
    ///
    /// The Annoying File Dependency in Encryption.Asymmetric
    /// Unfortunately, Microsoft chose to provide some System.Security.Cryptography functionality 
    /// through the existing COM-based CryptoAPI. Typically this is no big deal; lots of things in 
    /// .NET are delivered via COM interfaces. However, there is one destructive side effect in 
    /// this case: asymmetric encryption, which in my opinion should be an entirely in-memory 
    /// operation, has a filesystem "key container" dependency:
    /// 
    /// Even worse, this weird little "key container" file usually goes to the current user's folder! 
    /// I have specified a machine folder as documented in this Microsoft knowledge base article. 
    /// Every time we perform an asymmetric encryption operation, a file is created and then 
    /// destroyed in the C:\Documents and Settings\All Users\Application Data\Microsoft\Crypto\RSA\
    /// MachineKeys folder. It is simply unavoidable, which you can see for yourself by opening this 
    /// folder and watching what happens to it when you make asymmetric encryption calls. Make sure 
    /// whatever account .NET is running as (ASP.NET, etc.) has permission to this folder!
    ///-------------------------------------------------------------------------------------------------
    /// <remarks>
    public class Encryption
	{
		#region ------------- Properties ----------------------------------------------------------
        public string Password
        {
            get 
            {
                return _password; 
            }

            set
            {
                _password = value;
                if (_password.Length > 0)
				{
                    CheckPasswordLengthAndAdjust(_aes.LegalKeySizes);
                    CreateCryptoStreams(); 
				}
            }
        }
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private string                   _password;
        private AesCryptoServiceProvider _aes                = null;
        private ICryptoTransform         _encryptTransformer = null;
        private ICryptoTransform         _decryptTransformer = null;
        private CryptoStream             _encryptInStream    = null;
        private CryptoStream             _decryptInStream    = null;
        private MemoryStream             _encryptOutStream   = null;
        private MemoryStream             _decryptOutStream   = null;
        private bool                     _encryptFirstTime   = true;
        private bool                     _decryptFirstTime   = true;
        private bool                     _padPassword        = true;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
        public Encryption()
        {
            _aes = new AesCryptoServiceProvider();
            Password = "";
        }
		#endregion



		#region ------------- Methods -------------------------------------------------------------
        /// <summary>
        /// String mit dem AES-Algorithmus verschl�sseln
        /// </summary>
        /// 
        /// <example>
        /// Encryption enc = new Encryption();
        /// string Ausgabe = enc.Encrypt("Eingabe", Kennwort);
        /// </example>
        public string Encrypt(string input)
        {
            if (!_encryptFirstTime)
                CreateCryptoStreams();
            _encryptFirstTime = false;

           
            // Um am Ende nach dem Entschl�sseln genau die Anzahl an Zeichen 
            // rauszubekommen, die wir reinschieben, schreiben wir zun�chst die 
            // Zeichenanzahl in den Strom. Beim Entschl�sseln wissen wir das 
            // und k�nnen die Zahl wieder auslesen und damit den Block trimmen.
            input = String.Format("{0:D12}", input.Length) + input;


            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            int length = inputBytes.Length;


            // Unverschl�sselte Daten in den Strom schreiben
            _encryptInStream.Write(inputBytes, 0, length);


            // Da die Verschl�sselungsfunktion mit Bl�cken � 16 Bytes (InBlockSize) 
            // arbeitet, m�ssen wir die Eingabe immer auf ein Vielfaches von 16 bringen.
            // Sonst fehlen bis zu 15 Bytes im Chiffrat. (die letzten 15 Bytes)s
            PadToNextBlocksize(_encryptInStream, length, _encryptTransformer.InputBlockSize);

            // Verschl�sselte Daten auslesen
            byte[] Ausgabe = GetBytesFromStream(_encryptOutStream);
            return Convert.ToBase64String(Ausgabe);
        }

        /// <summary>
        /// String mit dem AES-Algorithmus entschl�sseln
        /// </summary>
        /// 
        /// <example>
        /// Encryption enc = new Encryption();
        /// string Ausgabe = enc.Decrypt("asdkjfhasidufzguiergt", Kennwort);
        /// </example>
        public string Decrypt(string input)
        {
            if (!_decryptFirstTime)
                CreateCryptoStreams(); // Reinitialisierung f�rs n�chste Mal
            _decryptFirstTime = false;

            byte[] inputBytes = Convert.FromBase64String(input);
            _decryptInStream.Write(inputBytes, 0, inputBytes.Length);


            // Auch f�r die die Entschl�sselung m�ssen wir mehr Bytes reinschieben
            // Ich habe das nicht n�cher untersucht. Wenn man 16 Bytes reinschiebt,
            // kommt nichts raus. Wenn man nochmal die Blockgr��e reinschiebt,
            // kommen die 16 Bytes raus.
            int a = _decryptTransformer.InputBlockSize;
            byte[] padder = new byte[a];
            _decryptInStream.Write(padder, 0, padder.Length);


            // Entschl�sselten Text vom Ausgabestrom lesen
            byte[] outputBytes = GetBytesFromStream(_decryptOutStream);
            string output = Encoding.UTF8.GetString(outputBytes);


            // Jetzt holen wir die Original-Stringl�nge (12 Ziffern) wieder raus
            // und trimmen damit den entschl�sselten String
            string lengthstring = output.Substring(0, 12);
            output = output.Remove(0, 12);
            int length = Convert.ToInt32(lengthstring);

            return output.Substring(0, length);
        }

		#endregion



		#region ------------- Implementation ------------------------------------------------------
        private void PadToNextBlocksize(CryptoStream stream, int length, int inBlockSize)
        {
            int next16ByteBarrier = ((length / inBlockSize) + 1) * inBlockSize;
            int difference = next16ByteBarrier - length;
            byte[] padder = new byte[difference];
            stream.Write(padder, 0, padder.Length);
        }

        private byte[] GetBytesFromStream(MemoryStream stream)
        {
            long byteCount = stream.Length;
            byte[] byteArray = new byte[byteCount];
            stream.Seek(0, SeekOrigin.Begin);
            int count = stream.Read(byteArray, 0, (int) byteCount);
            return byteArray;
        }

        private void CreateCryptoStreams()
        {
            _encryptOutStream = new MemoryStream();
            _decryptOutStream = new MemoryStream();

            // Die AES-Encryption m�chte gerne einen Initialisierungsvektor in 128 Bits L�nge haben
            string initializationVector = Password;
            if (initializationVector.Length > 128 / 8)
                initializationVector = initializationVector.Substring(0, 128 / 8);


            //---------------------------------------------------------------------------
            // Datentyp des �bergebenen Kennwort-Strings umwandeln
            // (Schl�ssel und Initialisierungsvektor f�r die Encryption)
            //---------------------------------------------------------------------------
            byte[] tdesKey = ASCIIEncoding.ASCII.GetBytes(Password);
            byte[] tdesIV  = ASCIIEncoding.ASCII.GetBytes(initializationVector);
            initializationVector = "";

            _encryptTransformer = _aes.CreateEncryptor(tdesKey, tdesIV);
            _decryptTransformer = _aes.CreateDecryptor(tdesKey, tdesIV);

            _encryptInStream = new CryptoStream(_encryptOutStream, _encryptTransformer, CryptoStreamMode.Write);
            _decryptInStream = new CryptoStream(_decryptOutStream, _decryptTransformer, CryptoStreamMode.Write);
        }

        /// <summary>
        /// Pr�fen, ob das �bergebene Kennwort eine passende L�nge hat
        /// </summary>
        ///
        /// <remarks>
        /// Die Encryptionsklasse sagt uns, wie die Kennwortl�nge sein muss
        /// Wenn gew�nscht, kann die Funktion das �bergebene Kennwort 
        /// auf die richtige L�nge bringen.
        /// </remarks>
        ///
        /// <example>
        /// Die Klasse sagt MinSize=128, MaxSize=192, SkipSize=64
        /// Das bedeutet, dass das Kennwort 128 oder 192 Bits lang sein muss.
        /// </example>
        /// 
        /// <param name="legalKeySizes">
        /// Hier in dieser Klasse finden wir die Informationen 
        /// �ber m�gliche Kennwortl�ngen.
        /// </param>
        /// 
        /// <returns>
        /// 0 - OK
        /// 1 = Das Kennwort hat keine f�r das Encryptionsverfahren erlaubte L�nge (zu lang oder zu kurz)
        /// 2 = Das Kennwort hat keine erlaubte L�nge und wurde angepa�t
        /// </returns>
        private void CheckPasswordLengthAndAdjust(KeySizes[] legalKeySizes)
        {
            if (_password.Length == 0)
                return;
            int passwordLengthInBits = _password.Length * 8;
            bool found = false;
            int maximumLengthInBits = 0;
            foreach (KeySizes size in legalKeySizes)
            {
                // Alle erlaubten Kennwortl�ngen durchprobieren
                for (int lengthInBits = size.MinSize; lengthInBits <= size.MaxSize; lengthInBits += size.SkipSize)
                {
                    if (lengthInBits > maximumLengthInBits)
                        maximumLengthInBits = lengthInBits;

                    if (passwordLengthInBits == lengthInBits)
                        found = true;
                }
                if (found)
                    break;
            }

            if (!found)
            {
                if (_padPassword)
                {
                    // Wir haben beim Testen der Kennwortl�ngen das Maximum aller erlaubten L�ngen gebildet.
                    // Wir schneiden das Kennwort auf die Maximall�nge ab
                    int maximumLength = maximumLengthInBits / 8;
                    if (_password.Length > maximumLength)
                        _password = _password.Substring(0,maximumLength);

                    // bzw. f�llen es auf, in dem wir die eigenen Zeichen immer wieder anh�ngen
                    else
                    {
                        while (_password.Length < maximumLength)
                        {
                            _password += _password;
                            if (_password.Length > maximumLength)
                                _password = _password.Substring(0, maximumLength);
                        }
                    }
                    return;
                }
                else
                    throw new IllegalPasswordLengthException("The password has no sufficient length for the encryption method (too long or too short)");
            }
        }
		#endregion
    }
}
