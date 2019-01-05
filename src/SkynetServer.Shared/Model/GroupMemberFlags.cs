using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Model
{
    [Flags]
    public enum GroupMemberFlags : byte
    {
        None = 0,
        Administrator = 1,
        /// <summary>
        /// Prohibit sending of chat messages
        /// </summary>
        NoContent = 2,
        /// <summary>
        /// Prohibit sending of receive and read confirmations
        /// </summary>
        NoMetadata = 4,
        /// <summary>
        /// User is invisible for other group members
        /// </summary>
        Invisible = 8
    }
}
