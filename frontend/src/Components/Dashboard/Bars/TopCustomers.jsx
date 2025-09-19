import React, { useEffect, useState } from 'react'
import Card from '../Card'
import CardContent from '../CardContent'
import { Bar, BarChart, CartesianGrid, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getTopCustomersData } from "../../../Services/TopCustomersService"
import formatNumber from '../../../Services/FormatNumberService'

const TopCustomers = ({ filters, showDetails}) => {

    const [data, setData] = useState([]);
    const [loading, setLoading] = useState(false);

    const fetchData = async() => {
            try{
                const fetchedData = await getTopCustomersData(filters);
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
    <Card>
        <CardContent>
            <header>
            <h2 className="chart-title">Top 5 Customers</h2>
            </header>
            <ResponsiveContainer width="100%" height={250}>
            <BarChart
                layout="vertical"
                data={data}
                margin={{ left: 50, right: 20 }}
            >
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis type="number" tickFormatter={!showDetails && formatNumber}/>
                <YAxis type="category" dataKey="cust_email" width={150} style={{fontSize: 10 }}/>
                <Tooltip formatter={!showDetails ? (value) => [`$${formatNumber(value)}`, "Sales"] : undefined} />
                <Bar dataKey="sales" fill="#75c5d7ff">
                    <LabelList formatter={(value) => value+"%"} dataKey="percentOfTotal" style={{ fill: '#000', fontSize: 12 }} />
                </Bar>
            </BarChart>
            </ResponsiveContainer>
        </CardContent>
    </Card>
  )
}

export default TopCustomers
