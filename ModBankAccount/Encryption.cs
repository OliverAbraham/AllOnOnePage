using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Abraham.Security
{
    /// <summary>
    /// Kennzeichnet eine unbrauchbare Kennwortlänge, um etwas zu verschlüsseln
    /// </summary>
    public class IllegalPasswordLengthException : Exception
    {
        /// <summary>
        /// Kennzeichnet eine unbrauchbare Kennwortlänge, um etwas zu verschlüsseln
        /// </summary>
        public IllegalPasswordLengthException() 
        { 
        }

        /// <summary>
        /// Kennzeichnet eine unbrauchbare Kennwortlänge, um etwas zu verschlüsseln
        /// </summary>
        public IllegalPasswordLengthException(string message) 
        { 
        }
    }


    /// <summary>
    /// Verschlüsselt und entschlüsselt texte mit dem AES-Algorithmus
    /// </summary>
    /// 
    /// <remarks>
    ///-------------------------------------------------------------------------------------------------
    ///
    ///                                 Oliver Abraham
    ///                              www.oliver-abraham.de
    ///                              mail@oliver-abraham.de
    ///
    ///              Klasse für die Verschlüsselung und Entschlüsselung von Daten
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
        /// String mit dem AES-Algorithmus verschlüsseln
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

           
            // Um am Ende nach dem Entschlüsseln genau die Anzahl an Zeichen 
            // rauszubekommen, die wir reinschieben, schreiben wir zunächst die 
            // Zeichenanzahl in den Strom. Beim Entschlüsseln wissen wir das 
            // und können die Zahl wieder auslesen und damit den Block trimmen.
            input = String.Format("{0:D12}", input.Length) + input;


            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            int length = inputBytes.Length;


            // Unverschlüsselte Daten in den Strom schreiben
            _encryptInStream.Write(inputBytes, 0, length);


            // Da die Verschlüsselungsfunktion mit Blöcken à 16 Bytes (InBlockSize) 
            // arbeitet, müssen wir die Eingabe immer auf ein Vielfaches von 16 bringen.
            // Sonst fehlen bis zu 15 Bytes im Chiffrat. (die letzten 15 Bytes)s
            PadToNextBlocksize(_encryptInStream, length, _encryptTransformer.InputBlockSize);

            // Verschlüsselte Daten auslesen
            byte[] Ausgabe = GetBytesFromStream(_encryptOutStream);
            return Convert.ToBase64String(Ausgabe);
        }

        /// <summary>
        /// String mit dem AES-Algorithmus entschlüsseln
        /// </summary>
        /// 
        /// <example>
        /// Encryption enc = new Encryption();
        /// string Ausgabe = enc.Decrypt("asdkjfhasidufzguiergt", Kennwort);
        /// </example>
        public string Decrypt(string input)
        {
            if (!_decryptFirstTime)
                CreateCryptoStreams(); // Reinitialisierung fürs nächste Mal
            _decryptFirstTime = false;

            byte[] inputBytes = Convert.FromBase64String(input);
            _decryptInStream.Write(inputBytes, 0, inputBytes.Length);


            // Auch für die die Entschlüsselung müssen wir mehr Bytes reinschieben
            // Ich habe das nicht nächer untersucht. Wenn man 16 Bytes reinschiebt,
            // kommt nichts raus. Wenn man nochmal die Blockgröße reinschiebt,
            // kommen die 16 Bytes raus.
            int a = _decryptTransformer.InputBlockSize;
            byte[] padder = new byte[a];
            _decryptInStream.Write(padder, 0, padder.Length);


            // Entschlüsselten Text vom Ausgabestrom lesen
            byte[] outputBytes = GetBytesFromStream(_decryptOutStream);
            string output = Encoding.UTF8.GetString(outputBytes);


            // Jetzt holen wir die Original-Stringlänge (12 Ziffern) wieder raus
            // und trimmen damit den entschlüsselten String
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

            // Die AES-Encryption möchte gerne einen Initialisierungsvektor in 128 Bits Länge haben
            string initializationVector = Password;
            if (initializationVector.Length > 128 / 8)
                initializationVector = initializationVector.Substring(0, 128 / 8);


            //---------------------------------------------------------------------------
            // Datentyp des übergebenen Kennwort-Strings umwandeln
            // (Schlüssel und Initialisierungsvektor für die Encryption)
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
        /// Prüfen, ob das übergebene Kennwort eine passende Länge hat
        /// </summary>
        ///
        /// <remarks>
        /// Die Encryptionsklasse sagt uns, wie die Kennwortlänge sein muss
        /// Wenn gewünscht, kann die Funktion das übergebene Kennwort 
        /// auf die richtige Länge bringen.
        /// </remarks>
        ///
        /// <example>
        /// Die Klasse sagt MinSize=128, MaxSize=192, SkipSize=64
        /// Das bedeutet, dass das Kennwort 128 oder 192 Bits lang sein muss.
        /// </example>
        /// 
        /// <param name="legalKeySizes">
        /// Hier in dieser Klasse finden wir die Informationen 
        /// über mögliche Kennwortlängen.
        /// </param>
        /// 
        /// <returns>
        /// 0 - OK
        /// 1 = Das Kennwort hat keine für das Encryptionsverfahren erlaubte Länge (zu lang oder zu kurz)
        /// 2 = Das Kennwort hat keine erlaubte Länge und wurde angepaßt
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
                // Alle erlaubten Kennwortlängen durchprobieren
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
                    // Wir haben beim Testen der Kennwortlängen das Maximum aller erlaubten Längen gebildet.
                    // Wir schneiden das Kennwort auf die Maximallänge ab
                    int maximumLength = maximumLengthInBits / 8;
                    if (_password.Length > maximumLength)
                        _password = _password.Substring(0,maximumLength);

                    // bzw. füllen es auf, in dem wir die eigenen Zeichen immer wieder anhängen
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
