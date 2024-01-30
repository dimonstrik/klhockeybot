select v.question ev
from voting v
where 1 = 1
    and v.messageid in (
        select vv.messageid
        from voting vv
        where vv.question like 'ИГРА%'
            or vv.question like 'ТРЕНЯ%'
        order by vv.messageid desc
        limit @replace_top_n
    )
order by v.messageid desc