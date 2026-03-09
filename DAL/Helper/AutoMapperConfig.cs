using AutoMapper;
using BLL.DTOs.Ec;
using DAL.Data.Models;

namespace DAL.Helper;

public class AutoMapperConfig : Profile
{
    public AutoMapperConfig()
    {
        CreateMap<EcDTO, Ec>();
        CreateMap<Ec, EcDTO>();
    }
}
