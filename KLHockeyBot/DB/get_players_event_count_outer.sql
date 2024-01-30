select sum(
        case
            when v.question like 'ТРЕНЯ%' then 1
            else 0
        end
    ) practice_cnt,
    sum(
        case
            when v.question like 'ИГРА%' then 1
            else 0
        end
    ) game_cnt,
    min(vo.name) name
from voting v
    join vote vo on v.messageid = vo.messageid
where vo.data = 'Да'
    and v.messageid in (
        select vv.messageid
        from voting vv
        where vv.question like 'ИГРА%'
            or vv.question like 'ТРЕНЯ%'
        order by vv.messageid desc
        limit @replace_top_n
    )
    and vo.userid = 0
group by vo.name
order by (practice_cnt + game_cnt) desc