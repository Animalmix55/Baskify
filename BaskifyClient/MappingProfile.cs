using AutoMapper;
using BaskifyClient.DTOs;
using BaskifyClient.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            Mapper.CreateMap<BasketModel, BasketDto>();
            Mapper.CreateMap<BasketPhotoModel, BasketPhotoDto>();
            Mapper.CreateMap<AuctionModel, AuctionDto>();
            Mapper.CreateMap<AuctionModel, LocationAuctionDto>(); //auction with location info
            Mapper.CreateMap<BasketModel, PrivBasketDto>();
            Mapper.CreateMap<TicketModel, TicketDto>();
            Mapper.CreateMap<UserModel, UserDto>();
            Mapper.CreateMap<UserModel, OrganizationDto>();
            Mapper.CreateMap<PaymentModel, PaymentDto>();
            Mapper.CreateMap<PaymentModel, ReceiptDto>();
            Mapper.CreateMap<PaymentMethodCard, CardDto>().ReverseMap(); //stripe casts
            Mapper.CreateMap<Address, AddressDto>().ReverseMap();
            Mapper.CreateMap<PaymentMethod, PaymentMethodDto>().ReverseMap();
            Mapper.CreateMap<BillingDetails, BillingDetailsDto>().ReverseMap();
            Mapper.CreateMap<IRSNonProfit, NonProfitDto>();
        }
    }
}
