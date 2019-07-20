using System;
using System.Collections.Generic;

namespace SkynetServer.Model
{
    public static class EnumExtensions
    {
        /// <summary>
        /// This method checks whether these <see cref="MessageFlags"/> have all required and no forbidden flags.
        /// </summary>
        /// <param name="value"><see cref="MessageFlags"/> to validate.</param>
        /// <param name="required"><see cref="MessageFlags"/> that have to be set.</param>
        /// <param name="allowed"><see cref="MessageFlags"/> that can be set.</param>
        public static bool AreValid(this MessageFlags value, MessageFlags required, MessageFlags allowed)
        {
            return (value & required) == required
                && (value | allowed) == allowed;
        }
    }
}
