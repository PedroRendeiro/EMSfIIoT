using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EMSfIIoT_API.Models
{
    public class MeasureDTO : IValidatableObject
    {

        /// <summary>
        /// Type of measure (Empty, Peak, Full)
        /// </summary>
        /// <example>1</example>
        public int MeasureTypeID { get; set; }

        /// <summary>
        /// Location where the measure was retrived
        /// </summary>
        /// <example>1</example>
        public int LocationID { get; set; }

        /// <summary>
        /// Value of the measure
        /// </summary>
        /// <example>4578</example>
        [Required]
        public uint Value { get; set; }

        /// <summary>
        /// Unit of the measure
        /// </summary>
        /// <example>kWh</example>
        public string Unit { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validate the MeasureTypeID
            if (!Enumerable.Range(1, 3).Contains(MeasureTypeID))
            {
                results.Add(new ValidationResult("The MeasureTypeID must be beetween 1 and 3", new string[] { "MeasureTypeID" }));
            }

            // Validate the LocationID
            if (!Enumerable.Range(1, 3).Contains(LocationID))
            {
                results.Add(new ValidationResult("The LocationID must be beetween 1 and 3", new string[] { "LocationID" }));
            }

            return results;
        }
    }

    public class Measure
    {

        /// <summary>
        /// Id of the database entry
        /// </summary>
        /// <example>5</example>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// TimeStamp of the insertion or last update of the entry
        /// </summary>
        /// <example>2020-04-22T14:28:16.083</example>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Type of measure (Peak, Full, etc)
        /// </summary>
        /// <example>1</example>
        public int MeasureTypeID { get; set; }

        /// <summary>
        /// Location where the measure was retrived
        /// </summary>
        /// <example>1</example>
        public long LocationID { get; set; }

        /// <summary>
        /// Value of the measure
        /// </summary>
        /// <example>4578</example>
        [Required]
        public long Value { get; set; }
        
        /// <summary>
        /// Unit of the measure
        /// </summary>
        /// <example>kWh</example>
        public string Unit { get; set; }
    }
}