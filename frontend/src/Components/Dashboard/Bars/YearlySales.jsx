import React, { useEffect, useState } from 'react'
import Card from '../Card'
import CardContent from '../CardContent'
import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getYearlySalesData } from '../../../Services/YearlySalesService'
import formatNumber from '../../../Services/FormatNumberService'

const YearlySales = ({ filters, showDetails}) => {

    const [data, setData] = useState([]);
    const [loading, setLoading] = useState(false);

    const fetchData = async() => {
            try{
                const fetchedData = await getYearlySalesData(filters);
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
                <h2 className="chart-title">Sales Trend</h2>
            </header>
            <ResponsiveContainer width="100%" height={250}>
                <LineChart data={data}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="year" />
                    <YAxis tickFormatter={!showDetails && formatNumber}/>
                    <Tooltip formatter={!showDetails ? (value) => formatNumber(value) : undefined}/>
                    <Line type="monotone" dataKey="totalQuantity" stroke="#0088FE" />
                </LineChart>
            </ResponsiveContainer>
        </CardContent>
    </Card>
  )
}

export default YearlySales
