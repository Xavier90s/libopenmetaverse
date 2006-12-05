/*
 * Copyright (c) 2006, Second Life Reverse Engineering Team
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without 
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the Second Life Reverse Engineering Team nor the names 
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Net;
using System.Xml.Serialization;

namespace libsecondlife
{
    /// <summary>
    /// A 128-bit Universally Unique Identifier, used throughout the Second
    /// Life networking protocol
    /// </summary>
    [Serializable]
    public class LLUUID : IXmlSerializable
	{
        /// <summary>The 16 bytes that make up the UUID</summary>
        protected byte[] data = new byte[16];

        /// <summary>Get a byte array of the 16 raw bytes making up the UUID</summary>
		public byte[] Data
		{
			get { return data; }
		}

        /// <summary>
        /// Default constructor
        /// </summary>
		public LLUUID()
		{
		}

        /// <summary>
        /// Constructor that takes a string UUID representation
        /// </summary>
        /// <param name="val">A string representation of a UUID, case 
        /// insensitive and can either be hyphenated or non-hyphenated</param>
        /// <example>LLUUID("11f8aa9c-b071-4242-836b-13b7abe0d489")</example>
		public LLUUID(string val)
		{
			if (val.Length == 36) val = val.Replace("-", "");
			
			if (val.Length != 32) throw new Exception("Malformed data passed to LLUUID constructor: " + val);

			for(int i = 0; i < 16; ++i)
			{
				data[i] = Convert.ToByte(val.Substring(i * 2, 2), 16);
			}
		}

        /// <summary>
        /// Constructor that takes a byte array containing a UUID
        /// </summary>
        /// <param name="byteArray">Byte array containing a 16 byte UUID</param>
        /// <param name="pos">Beginning offset in the array</param>
		public LLUUID(byte[] byteArray, int pos)
		{
			Array.Copy(byteArray, pos, data, 0, 16);
		}

        /// <summary>
        /// Returns the raw bytes for this UUID
        /// </summary>
        /// <returns>A 16 byte array containing this UUID</returns>
        public byte[] GetBytes()
        {
            return data;
        }

		/// <summary>
		/// Calculate an LLCRC (cyclic redundancy check) for this LLUUID
		/// </summary>
		/// <returns>The CRC checksum for this LLUUID</returns>
		public uint CRC() 
		{
			uint retval = 0;

			retval += (uint)((Data[3] << 24) + (Data[2] << 16) + (Data[1] << 8) + Data[0]);
			retval += (uint)((Data[7] << 24) + (Data[6] << 16) + (Data[5] << 8) + Data[4]);
			retval += (uint)((Data[11] << 24) + (Data[10] << 16) + (Data[9] << 8) + Data[8]);
			retval += (uint)((Data[15] << 24) + (Data[14] << 16) + (Data[13] << 8) + Data[12]);

			return retval;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public static LLUUID Random()
		{
			return new LLUUID(Guid.NewGuid().ToByteArray(), 0);
		}

        /// <summary>
        /// Required implementation for XML serialization
        /// </summary>
        /// <returns>null</returns>
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Deserializes an XML UUID
        /// </summary>
        /// <param name="reader">XmlReader containing the UUID to deserialize</param>
        public void ReadXml(System.Xml.XmlReader reader)
        {
            string val = reader.ReadString();

            if (val.Length == 36) val = val.Replace("-", "");

            if (val.Length != 32) throw new Exception("Malformed data passed to LLUUID constructor: " + val);

            for (int i = 0; i < 16; ++i)
            {
                data[i] = Convert.ToByte(val.Substring(i * 2, 2), 16);
            }

            //reader.MoveToContent();
        }

        /// <summary>
        /// Serialize this UUID to XML
        /// </summary>
        /// <param name="writer">XmlWriter to serialize to</param>
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteString(this.ToString());
        }

        /// <summary>
        /// Return a hash code for this UUID, used by .NET for hash tables
        /// </summary>
        /// <returns>An integer composed of all the UUID bytes XORed together</returns>
		public override int GetHashCode()
		{
            int hash = data[0];

            for (int i = 1; i < 16; i++)
            {
                hash ^= data[i];
            }

			return hash;
		}

        /// <summary>
        /// Comparison function
        /// </summary>
        /// <param name="o">An object to compare to this UUID</param>
        /// <returns>False if the object is not an LLUUID, true if it is and
        /// byte for byte identical to this</returns>
		public override bool Equals(object o)
		{
			if (!(o is LLUUID)) return false;

			LLUUID uuid = (LLUUID)o;

			for (int i = 0; i < 16; ++i)
			{
				if (Data[i] != uuid.Data[i]) return false;
			}

			return true;
		}

        /// <summary>
        /// Equals operator
        /// </summary>
        /// <param name="lhs">First LLUUID for comparison</param>
        /// <param name="rhs">Second LLUUID for comparison</param>
        /// <returns>True if the UUIDs are byte for byte equal, otherwise false</returns>
		public static bool operator==(LLUUID lhs, LLUUID rhs)
		{
            // If both are null, or both are same instance, return true
            if (System.Object.ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
            {
                return false;
            }

			for (int i = 0; i < 16; ++i)
			{
				if (lhs.Data[i] != rhs.Data[i]) return false;
			}

			return true;
		}

        /// <summary>
        /// Not equals operator
        /// </summary>
        /// <param name="lhs">First LLUUID for comparison</param>
        /// <param name="rhs">Second LLUUID for comparison</param>
        /// <returns>True if the UUIDs are not equal, otherwise true</returns>
		public static bool operator!=(LLUUID lhs, LLUUID rhs)
		{
			return !(lhs == rhs);
		}

        /// <summary>
        /// XOR operator
        /// </summary>
        /// <param name="lhs">First LLUUID</param>
        /// <param name="rhs">Second LLUUID</param>
        /// <returns>A UUID that is a XOR combination of the two input UUIDs</returns>
        public static LLUUID operator ^(LLUUID lhs, LLUUID rhs)
        {
            LLUUID returnUUID = new LLUUID();

            for (int count = 0; count < returnUUID.Data.Length; count++)
            {
                returnUUID.Data[count] = (byte)(lhs.Data[count] ^ rhs.Data[count]);
            }

            return returnUUID;
        }

        /// <summary>
        /// String typecasting operator
        /// </summary>
        /// <param name="val">A UUID in string form. Case insensitive, 
        /// hyphenated or non-hyphenated</param>
        /// <returns>A UUID built from the string representation</returns>
        public static implicit operator LLUUID(string val)
		{
			return new LLUUID(val);
		}

        /// <summary>
        /// Get a string representation of this UUID
        /// </summary>
        /// <returns>A string representation of this UUID, lowercase and 
        /// without hyphens</returns>
        /// <example>11f8aa9cb0714242836b13b7abe0d489</example>
		public override string ToString()
		{
			string uuid = String.Empty;

			for (int i = 0; i < 16; ++i)
			{
				uuid += Data[i].ToString("x2");
			}

			return uuid;
		}

        /// <summary>
        /// Get a hyphenated string representation of this UUID
        /// </summary>
        /// <returns>A string representation of this UUID, lowercase and 
        /// with hyphens</returns>
        /// <example>11f8aa9c-b071-4242-836b-13b7abe0d489</example>
		public string ToStringHyphenated()
		{
			string uuid = String.Empty;

			for (int i = 0; i < 16; ++i)
			{
				uuid += Data[i].ToString("x2");
			}
			uuid = uuid.Insert(20,"-");
			uuid = uuid.Insert(16,"-");
			uuid = uuid.Insert(12,"-");
			uuid = uuid.Insert(8,"-");
			
			return uuid;
		}

        /// <summary>
        /// An LLUUID with a value of all zeroes
        /// </summary>
        public static readonly LLUUID Zero = new LLUUID();
	}

    /// <summary>
    /// A three-dimensional vector with floating-point values
    /// </summary>
    [Serializable]
	public class LLVector3
	{
        /// <summary>X value</summary>
		[XmlAttribute("x")] public float X;
        /// <summary>Y value</summary>
        [XmlAttribute("y")] public float Y;
        /// <summary>Z value</summary>
        [XmlAttribute("z")] public float Z;

        /// <summary>
        /// Default constructor, all values are set to 0.0
        /// </summary>
		public LLVector3()
		{
			X = Y = Z = 0.0f;
		}
		
        /// <summary>
        /// Constructor, builds a single-precision vector from a 
        /// double-precision one
        /// </summary>
        /// <param name="vector">A double-precision vector</param>
		public LLVector3(LLVector3d vector)
		{
			X = (float)vector.X;
			Y = (float)vector.Y;
			Z = (float)vector.Z;
		}

        /// <summary>
        /// Constructor, builds a vector from a byte array
        /// </summary>
        /// <param name="byteArray">Byte array containing a 12 byte vector</param>
        /// <param name="pos">Beginning position in the byte array</param>
		public LLVector3(byte[] byteArray, int pos)
		{
            if (!BitConverter.IsLittleEndian)
            {
                byte[] newArray = new byte[12];
                Array.Copy(byteArray, pos, newArray, 0, 12);

                Array.Reverse(newArray, 0, 4);
                Array.Reverse(newArray, 4, 4);
                Array.Reverse(newArray, 8, 4);

                X = BitConverter.ToSingle(newArray, 0);
                Y = BitConverter.ToSingle(newArray, 4);
                Z = BitConverter.ToSingle(newArray, 8);
            }
            else
            {
                X = BitConverter.ToSingle(byteArray, pos);
                Y = BitConverter.ToSingle(byteArray, pos + 4);
                Z = BitConverter.ToSingle(byteArray, pos + 8);
            }
		}

        /// <summary>
        /// Constructor, builds a vector for individual float values
        /// </summary>
        /// <param name="x">X value</param>
        /// <param name="y">Y value</param>
        /// <param name="z">Z value</param>
		public LLVector3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

        /// <summary>
        /// Returns the raw bytes for this vector
        /// </summary>
        /// <returns>A 12 byte array containing X, Y, and Z</returns>
		public byte[] GetBytes()
		{
			byte[] byteArray = new byte[12];

			Array.Copy(BitConverter.GetBytes(X), 0, byteArray, 0, 4);
			Array.Copy(BitConverter.GetBytes(Y), 0, byteArray, 4, 4);
			Array.Copy(BitConverter.GetBytes(Z), 0, byteArray, 8, 4);

			if(!BitConverter.IsLittleEndian) {
				Array.Reverse(byteArray, 0, 4);
				Array.Reverse(byteArray, 4, 4);
				Array.Reverse(byteArray, 8, 4);
			}

			return byteArray;
		}

        /// <summary>
        /// Get a formatted string representation of the vector
        /// </summary>
        /// <returns>A string representation of the vector, similar to the LSL
        /// vector to string conversion in Second Life</returns>
		public override string ToString()
		{
			return "<" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ">";
		}

        /// <summary>
        /// A hash of the vector, used by .NET for hash tables
        /// </summary>
        /// <returns>The hashes of the individual components XORed together</returns>
		public override int GetHashCode()
		{
			return (X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode());
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
		public override bool Equals(object o)
		{
			if (!(o is LLVector3)) return false;

			LLVector3 vector = (LLVector3)o;

			return (X == vector.X && Y == vector.Y && Z == vector.Z);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
		public static bool operator==(LLVector3 lhs, LLVector3 rhs)
		{
            // If both are null, or both are same instance, return true
            if (System.Object.ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
            {
                return false;
            }

			return (lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
		public static bool operator!=(LLVector3 lhs, LLVector3 rhs)
		{
			return !(lhs == rhs);
		}

        public static LLVector3 operator +(LLVector3 lhs, LLVector3 rhs)
        {
            return new LLVector3(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static LLVector3 operator -(LLVector3 lhs, LLVector3 rhs)
        {
            return new LLVector3(lhs.X - rhs.X,lhs.Y - rhs.Y, lhs.Z - rhs.Z);
        }

        public static LLVector3 operator *(LLVector3 vec, LLQuaternion quat)
        {
            LLQuaternion vq = new LLQuaternion(vec.X, vec.Y, vec.Z, 0);
            LLQuaternion nq = new LLQuaternion(-quat.X, -quat.Y, -quat.Z, quat.W);

            LLQuaternion result = (quat * vq) * nq;

            return new LLVector3(result.X, result.Y, result.Z);
        }

        /// <summary>
        /// An LLVector3 with a value of 0,0,0
        /// </summary>
        public readonly static LLVector3 Zero = new LLVector3();
	}

    /// <summary>
    /// A double-precision three-dimensional vector
    /// </summary>
    [Serializable]
	public class LLVector3d
	{
        /// <summary>X value</summary>
        [XmlAttribute("x")] public double X;
        /// <summary>Y value</summary>
        [XmlAttribute("y")] public double Y;
        /// <summary>Z value</summary>
        [XmlAttribute("z")] public double Z;

        /// <summary>
        /// Default constructor, sets all the values to 0.0
        /// </summary>
		public LLVector3d()
		{
			X = Y = Z = 0.0d;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
		public LLVector3d(double x, double y, double z)
		{
			X = x;
			Y = y;
			Z = z;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="byteArray"></param>
        /// <param name="pos"></param>
		public LLVector3d(byte[] byteArray, int pos)
		{
            if (!BitConverter.IsLittleEndian)
            {
                byte[] newArray = new byte[24];
                Array.Copy(byteArray, pos, newArray, 0, 24);

                Array.Reverse(newArray, 0, 8);
                Array.Reverse(newArray, 8, 8);
                Array.Reverse(newArray, 16, 8);

                X = BitConverter.ToDouble(newArray, 0);
                Y = BitConverter.ToDouble(newArray, 8);
                Z = BitConverter.ToDouble(newArray, 16);
            }
            else
            {
                X = BitConverter.ToDouble(byteArray, pos);
                Y = BitConverter.ToDouble(byteArray, pos + 8);
                Z = BitConverter.ToDouble(byteArray, pos + 16);
            }
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public byte[] GetBytes()
		{
			byte[] byteArray = new byte[24];

			Array.Copy(BitConverter.GetBytes(X), 0, byteArray, 0, 8);
			Array.Copy(BitConverter.GetBytes(Y), 0, byteArray, 8, 8);
			Array.Copy(BitConverter.GetBytes(Z), 0, byteArray, 16, 8);

			if(!BitConverter.IsLittleEndian)
            {
				Array.Reverse(byteArray, 0, 8);
				Array.Reverse(byteArray, 8, 8);
				Array.Reverse(byteArray, 16, 8);
			}

			return byteArray;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public override string ToString()
		{
			return "<" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ">";
		}

        /// <summary>
        /// An LLVector3d with a value of 0,0,0
        /// </summary>
        public static readonly LLVector3d Zero = new LLVector3d();
	}

    /// <summary>
    /// A four-dimensional vector
    /// </summary>
    [Serializable]
	public class LLVector4
	{
        /// <summary></summary>
        [XmlAttribute("x")] public float X;
        /// <summary></summary>
        [XmlAttribute("y")] public float Y;
        /// <summary></summary>
        [XmlAttribute("z")] public float Z;
        /// <summary></summary>
        [XmlAttribute("s")] public float S;

        /// <summary>
        /// 
        /// </summary>
		public LLVector4()
		{
			X = Y = Z = S = 0.0f;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="byteArray"></param>
        /// <param name="pos"></param>
		public LLVector4(byte[] byteArray, int pos)
		{
            if (!BitConverter.IsLittleEndian)
            {
                byte[] newArray = new byte[16];
                Array.Copy(byteArray, pos, newArray, 0, 16);

                Array.Reverse(newArray, 0, 4);
                Array.Reverse(newArray, 4, 4);
                Array.Reverse(newArray, 8, 4);
                Array.Reverse(newArray, 12, 4);

                X = BitConverter.ToSingle(newArray, 0);
                Y = BitConverter.ToSingle(newArray, 4);
                Z = BitConverter.ToSingle(newArray, 8);
                S = BitConverter.ToSingle(newArray, 12);
            }
            else
            {
                X = BitConverter.ToSingle(byteArray, pos);
                Y = BitConverter.ToSingle(byteArray, pos + 4);
                Z = BitConverter.ToSingle(byteArray, pos + 8);
                S = BitConverter.ToSingle(byteArray, pos + 12);
            }
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public byte[] GetBytes()
		{
			byte[] byteArray = new byte[16];

			Array.Copy(BitConverter.GetBytes(X), 0, byteArray, 0, 4);
			Array.Copy(BitConverter.GetBytes(Y), 0, byteArray, 4, 4);
			Array.Copy(BitConverter.GetBytes(Z), 0, byteArray, 8, 4);
			Array.Copy(BitConverter.GetBytes(S), 0, byteArray, 12, 4);

			if(!BitConverter.IsLittleEndian) {
				Array.Reverse(byteArray, 0, 4);
				Array.Reverse(byteArray, 4, 4);
				Array.Reverse(byteArray, 8, 4);
				Array.Reverse(byteArray, 12, 4);
			}

			return byteArray;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public override string ToString()
		{
			return "<" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ", " + S.ToString() + ">";
		}

        /// <summary>
        /// An LLVector4 with a value of 0,0,0,0
        /// </summary>
        public readonly static LLVector4 Zero = new LLVector4();
	}

    /// <summary>
    /// A quaternion, used for rotations
    /// </summary>
    [Serializable]
	public class LLQuaternion
	{
        /// <summary>X value</summary>
        [XmlAttribute("x")] public float X;
        /// <summary>Y value</summary>
        [XmlAttribute("y")] public float Y;
        /// <summary>Z value</summary>
        [XmlAttribute("z")] public float Z;
        /// <summary>W value</summary>
        [XmlAttribute("w")] public float W;

        /// <summary>
        /// Default constructor, initializes to no rotation (0,0,0,1)
        /// </summary>
		public LLQuaternion()
		{
			X = Y = Z = 0.0f;
            W = 1.0f;
		}

        /// <summary>
        /// Build a quaternion object from a byte array
        /// </summary>
        /// <param name="byteArray">The source byte array</param>
        /// <param name="pos">Offset in the byte array to start reading at</param>
        /// <param name="normalized">Whether the source data is normalized or
        /// not. If this is true 12 bytes will be read, otherwise 16 bytes will
        /// be read.</param>
        public LLQuaternion(byte[] byteArray, int pos, bool normalized)
        {
            if (!normalized)
            {
                if (!BitConverter.IsLittleEndian)
                {
                    byte[] newArray = new byte[16];
                    Array.Copy(byteArray, pos, newArray, 0, 16);

                    Array.Reverse(newArray, 0, 4);
                    Array.Reverse(newArray, 4, 4);
                    Array.Reverse(newArray, 8, 4);
                    Array.Reverse(newArray, 12, 4);

                    X = BitConverter.ToSingle(newArray, 0);
                    Y = BitConverter.ToSingle(newArray, 4);
                    Z = BitConverter.ToSingle(newArray, 8);
                    W = BitConverter.ToSingle(newArray, 12);
                }
                else
                {
                    X = BitConverter.ToSingle(byteArray, pos);
                    Y = BitConverter.ToSingle(byteArray, pos + 4);
                    Z = BitConverter.ToSingle(byteArray, pos + 8);
                    W = BitConverter.ToSingle(byteArray, pos + 12);
                }
            }
            else
            {
                if (!BitConverter.IsLittleEndian)
                {
                    byte[] newArray = new byte[12];
                    Array.Copy(byteArray, pos, newArray, 0, 12);

                    Array.Reverse(newArray, 0, 4);
                    Array.Reverse(newArray, 4, 4);
                    Array.Reverse(newArray, 8, 4);

                    X = BitConverter.ToSingle(newArray, 0);
                    Y = BitConverter.ToSingle(newArray, 4);
                    Z = BitConverter.ToSingle(newArray, 8);
                }
                else
                {
                    X = BitConverter.ToSingle(byteArray, pos);
                    Y = BitConverter.ToSingle(byteArray, pos + 4);
                    Z = BitConverter.ToSingle(byteArray, pos + 8);
                }

                float xyzsum = 1 - X * X - Y * Y - Z * Z;
                W = (xyzsum > 0) ? (float)Math.Sqrt(xyzsum) : 0;
            }
        }

        /// <summary>
        /// Build a quaternion from normalized float values
        /// </summary>
        /// <param name="x">X value from -1.0 to 1.0</param>
        /// <param name="y">Y value from -1.0 to 1.0</param>
        /// <param name="z">Z value from -1.0 to 1.0</param>
        public LLQuaternion(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;

            float xyzsum = 1 - X * X - Y * Y - Z * Z;
            W = (xyzsum > 0) ? (float)Math.Sqrt(xyzsum) : 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
		public LLQuaternion(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public byte[] GetBytes()
		{
            byte[] bytes = new byte[12];
            float norm;

            norm = (float)Math.Sqrt(X*X + Y*Y + Z*Z + W*W);

            if (norm != 0)
            {
                norm = 1 / norm;

                Array.Copy(BitConverter.GetBytes(norm * X), 0, bytes, 0, 4);
                Array.Copy(BitConverter.GetBytes(norm * Y), 0, bytes, 4, 4);
                Array.Copy(BitConverter.GetBytes(norm * Z), 0, bytes, 8, 4);

                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes, 0, 4);
                    Array.Reverse(bytes, 4, 4);
                    Array.Reverse(bytes, 8, 4);
                }
            }
            else
            {
                throw new Exception("Quaternion " + this.ToString() + " normalized to zero");
            }

			return bytes;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public override bool Equals(object o)
        {
            if (!(o is LLQuaternion)) return false;

            LLQuaternion quaternion = (LLQuaternion)o;

            return X == quaternion.X && Y == quaternion.Y && Z == quaternion.Z && W == quaternion.W;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator ==(LLQuaternion lhs, LLQuaternion rhs)
        {
            // If both are null, or both are same instance, return true
            if (System.Object.ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
            {
                return false;
            }

            // Return true if the fields match:
            return lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z && lhs.W == rhs.W;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator !=(LLQuaternion lhs, LLQuaternion rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static LLQuaternion operator *(LLQuaternion lhs, LLQuaternion rhs)
        {
            LLQuaternion ret = new LLQuaternion();
            ret.W = lhs.W * rhs.W - lhs.X * rhs.X - lhs.Y * rhs.Y - lhs.Z * rhs.Z;
            ret.X = lhs.W * rhs.X + lhs.X * rhs.W + lhs.Y * rhs.Z - lhs.Z * rhs.Y;
            ret.Y = lhs.W * rhs.Y + lhs.Y * rhs.W + lhs.Z * rhs.X - lhs.X * rhs.Z;
            ret.Z = lhs.W * rhs.Z + lhs.Z * rhs.W + lhs.X * rhs.Y - lhs.Y * rhs.X;
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public override string ToString()
		{
			return "<" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ", " + W.ToString() + ">";
		}

        /// <summary>
        /// An LLQuaternion with a value of 0,0,0,1
        /// </summary>
        public readonly static LLQuaternion Identity = new LLQuaternion();
	}
}
