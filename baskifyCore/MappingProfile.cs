using AutoMapper;
using baskifyCore.DTOs;
using baskifyCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            Mapper.CreateMap<BasketModel, BasketDto>();
            Mapper.CreateMap<BasketPhotoModel, BasketPhotoDto>();
            Mapper.CreateMap<AuctionModel, AuctionDto>();
            Mapper.CreateMap<AuctionModel, LocationAuctionDto>(); //auction with location info
            Mapper.CreateMap<BasketModel, OrgBasketDto>();
            Mapper.CreateMap<TicketModel, TicketDto>();
            Mapper.CreateMap<UserModel, UserDto>();
            Mapper.CreateMap<UserModel, OrganizationDto>();
        }
    }
}
