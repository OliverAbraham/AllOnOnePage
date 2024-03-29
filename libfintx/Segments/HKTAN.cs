﻿/*	
 * 	
 *  This file is part of libfintx.
 *  
 *  Copyright (c) 2016 - 2020 Torsten Klinger
 * 	E-Mail: torsten.klinger@googlemail.com
 * 	
 * 	libfintx is free software; you can redistribute it and/or
 *	modify it under the terms of the GNU Lesser General Public
 * 	License as published by the Free Software Foundation; either
 * 	version 2.1 of the License, or (at your option) any later version.
 *	
 * 	libfintx is distributed in the hope that it will be useful,
 * 	but WITHOUT ANY WARRANTY; without even the implied warranty of
 * 	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * 	Lesser General Public License for more details.
 *	
 * 	You should have received a copy of the GNU Lesser General Public
 * 	License along with libfintx; if not, write to the Free Software
 * 	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 * 	
 */

using System;

namespace libfintx
{
    public static class HKTAN
    {
        /// <summary>
        /// HKTAN Segment (e.g. HKIDN)
        /// </summary>
        public static string SegmentId { get; set; }

        /// <summary>
        /// Set tan process
        /// </summary>
        /// <param name="segments"></param>
        /// <returns></returns>
        public static string Init_HKTAN(FinTsClient client, string segments)
        {
            if (String.IsNullOrEmpty(client.HITAB)) // TAN Medium Name not set
            {
                if (client.HITANS.Substring(0, 3).Equals("6+4"))
                    segments = segments + "HKTAN:" + client.SEGNUM + ":" + client.HITANS + "+" + SegmentId + "'";
                else
                    segments = segments + "HKTAN:" + client.SEGNUM + ":" + client.HITANS + "+'";
            }
            else // TAN Medium Name set
            {
                // Version 3, Process 4
                if (client.HITANS.Substring(0, 3).Equals("3+4"))
                    segments = segments + "HKTAN:" + client.SEGNUM + ":" + client.HITANS + "++++++++" + client.HITAB + "'";
                // Version 4, Process 4
                if (client.HITANS.Substring(0, 3).Equals("4+4"))
                    segments = segments + "HKTAN:" + client.SEGNUM + ":" + client.HITANS + "+++++++++" + client.HITAB + "'";
                // Version 5, Process 4
                if (client.HITANS.Substring(0, 3).Equals("5+4"))
                    segments = segments + "HKTAN:" + client.SEGNUM + ":" + client.HITANS + "+++++++++++" + client.HITAB + "'";
                // Version 6, Process 4
                if (client.HITANS.Substring(0, 3).Equals("6+4"))
                {
                    segments = segments + "HKTAN:" + client.SEGNUM + ":" + client.HITANS + "+" + SegmentId + "+++++++++" + client.HITAB + "'";
                }
            }

            return segments;
        }
    }
}
