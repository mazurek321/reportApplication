import React, { useEffect, useState } from 'react'
import "../Dashboard.css"
import Card from '../Card'
import CardContent from '../CardContent'
import { getTopBarData } from '../../../Services/TopBarService'
import profit from "../../../assets/profit.png";
import customers from "../../../assets/customers.png";
import sales from "../../../assets/sales.png";
import total_cost from "../../../assets/total_cost.png";
import formatNumber from '../../../Services/FormatNumberService'

const TopBar = ({ filters, showDetails }) => {
    const [data, setData] = useState([]);
    const [loading, setLoading] = useState(false);

    const fetchData = async() => {
            try{
                const fetchedData = await getTopBarData(filters);
                setData(fetchedData);
            }catch(error)
            {
                console.log("Error: " + error)
                setLoading(false);
            }
            finally
            {
                setLoading(false);
            }
        }

    useEffect(()=>{
        setLoading(true);
        fetchData();
    }, [filters])

  return (
    loading ? <div>Loading...</div> :
    <div className="kpi-grid">
        <Card>
            <CardContent>
                <div className="kpi-row">
                    <span className="kpi-bar blue"></span>
                    <div className="kpi-text">
                    <p className="kpi-label">Total Amount</p>
                    <p className="kpi-value">${!showDetails ? formatNumber(data.totalSales) : data.totalSales}</p>
                    </div>
                    <span className="kpi-icon blue">
                    <img src={sales} />
                    </span>
                </div>
            </CardContent>
        </Card>
        <Card>
            <CardContent>
                <div className="kpi-row">
                    <span className="kpi-bar green"></span>
                    <div className="kpi-text">
                    <p className="kpi-label">Customers</p>
                    <p className="kpi-value">{!showDetails ? formatNumber(data.customers) : data.customers}</p>
                    </div>
                    <span className="kpi-icon green">
                    <img src={customers} />
                    </span>
                </div>
            </CardContent>
        </Card>
        <Card>
            <CardContent>
                <div className="kpi-row">
                    <span className="kpi-bar yellow"></span>
                    <div className="kpi-text">
                    <p className="kpi-label">Total cost</p>
                    <p className="kpi-value">${!showDetails ? formatNumber(data.totalCost) : data.totalCost}</p>
                    </div>
                    <span className="kpi-icon yellow">
                    <img src={total_cost}/>
                    </span>
                </div>
            </CardContent>
        </Card>
        <Card>
            <CardContent>
                <div className="kpi-row">
                    <span className="kpi-bar red"></span>
                    <div className="kpi-text">
                    <p className="kpi-label">Profit</p>
                    <div className="flex">
                        <p className="kpi-value">${!showDetails ? formatNumber(data.profit) : data.profit}</p>
                        <p className="kpi-value growth">{data.profitPercent > 0 && "+"}{data.profitPercent} %</p>
                    </div>
                    </div>
                    <span className="kpi-icon red">
                    <img src={profit}/>
                    </span>
                </div>
            </CardContent>
        </Card>
    </div>
  )
}

export default TopBar
