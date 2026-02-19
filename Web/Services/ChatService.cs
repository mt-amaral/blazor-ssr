using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Context;
using Web.Dto;
using Web.Dto.Account;
using Web.Entity.Identity;
using Web.Services.Abstractions;

namespace Web.Services;

public class ChatService(ApplicationDbContext context) :  IChatService
{
    
    
}
