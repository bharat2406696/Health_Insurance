﻿// Models/Employee.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding; // Required for [BindNever]

namespace Health_Insurance.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name can only contain alphabets and spaces.")] // NEW VALIDATION
        public string Name { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Phone number can only contain numbers.")] // NEW VALIDATION
        public string Phone { get; set; }

        public string Address { get; set; }

        [StringLength(50)]
        public string Designation { get; set; }

        [Required(ErrorMessage = "Organization is required.")]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        [BindNever]
        public virtual Organization? Organization { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50)]
        public string Username { get; set; }

        [StringLength(256)]
        public string PasswordHash { get; set; }
    }
}


