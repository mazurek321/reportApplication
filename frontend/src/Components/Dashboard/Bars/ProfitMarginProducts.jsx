import React, { useEffect, useState } from 'react'
import { getProfitMarginProducts } from "../../../Services/ProfitMarginService" 
import Card from '../Card';
import CardContent from '../CardContent';
import { Bar, BarChart, CartesianGrid, Cell, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import formatNumber from '../../../Services/FormatNumberService';

const ProfitMarginProducts = ({ filters, showDetails}) => {

    const [data, setData] = useState([]);
    const [loading, setLoading] = useState(false);

    const fetchData = async() => {
            try{
                const fetchedData = await getProfitMarginProducts(filters);
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

    const getMarginColor = (margin) => {
        if (margin >= 15) return "#00C49F";
        if (margin >= 10) return "#FFBB28";
        return "#FF4C4C";
    };

  return (
    <Card>
        <CardContent>
            <header>
                <h2 className="chart-title">Top Profit Margin by Product</h2>
            </header>
            <ResponsiveContainer width="100%" height={250}>
                <BarChart data={data} margin={{ top: 20 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="product" angle={8}  style={{ fontSize: 8 }}  tick={{ fontSize: 12, dy: 5 }}/>
                <YAxis unit="%" />
                <Tooltip formatter={(value) => [`${value}%`, "Margin"]} />
                <Bar dataKey="profitPercent">
                    {data.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={getMarginColor(entry.profitPercent)} />
                    ))}
                    <LabelList 
                        dataKey="profitPercent" 
                        position="top" 
                        formatter={(value) => `${value.toFixed(2)}%`} 
                    />
                    <LabelList 
                        dataKey="totalQuantity" 
                        position="bottom" 
                        offset={-10} 
                        formatter={!showDetails ? (value) => formatNumber(value) : undefined} 
                        style={{ fontSize: 10, fill: '#000', fontWeight: 'bold' }}
                    />
                    </Bar>
                </BarChart>
            </ResponsiveContainer>
        </CardContent>
    </Card>
  )
}

export default ProfitMarginProducts
