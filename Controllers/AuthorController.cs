using BookStoresWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStoresWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthorController : ControllerBase
    {
       

        [HttpGet]
        public IEnumerable<Author> Get()
        {
            using (var context = new BookStoresDBContext())
            {
                //To get the list of all Authors
                return context.Authors.ToList();

                //Add a new Author to the database
                //Author author = new Author();
                //author.FirstName = "John";
                //author.LastName = "Smith";

                //context.Author.Add(author);

                //To Update the Author in the database
                //Author author = context.Author.Where(Auth => Auth.FirstName == "John").FirstOrDefault();
                //author.Phone = "777-777-7777";


                //To remove the author 
                //Author author = context.Author.Where(Auth => Auth.FirstName == "John").FirstOrDefault();
                //context.Author.Remove(author);



                //context.SaveChanges();

                ////To get the list by Id
                //return context.Author.Where(Auth => Auth.FirstName == "John").ToList();
            }
         
        }
    }
}
