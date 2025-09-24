import React, { useEffect, useState } from 'react'
import Card from '../Card'
import CardContent from '../CardContent'
import formatNumber from '../../../Services/FormatNumberService'
import { Bar, BarChart, CartesianGrid, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis, LabelList } from 'recharts'
import { getTopProducts } from "../../../Services/TopProductsService"

const TopProducts = ({ filters, showDetails}) => {
    const [data, setData] = useState([]);
    const [loading, setLoading] = useState(false);
    const [quantity, setQuantity] = useState(4);

    const fetchData = async() => {
        try {
            const fetchedData = await getTopProducts(filters);
            setData(fetchedData);
        } catch(error) {
            console.log("Error: " + error)
        } finally {
            setLoading(false);
        }
    }
    
    useEffect(() => {
        setLoading(true);
        fetchData();
    }, [filters])

    const getMarginColor = (margin) => {
        if (margin >= 50000) return "#00C49F";
        if (margin >= 25000) return "#FFBB28";
        return "#FF4C4C";
    };

    return (
        <Card>
            <CardContent>
                <header>
                    <h2 className="chart-title">Top Sell Products</h2>
                </header>
                <ResponsiveContainer width="100%" height={250}>
                    <BarChart data={data} margin={{ top: 20 }}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="product" interval={0} angle={10}  tick={{ fontSize: 12, dy: 5 }}/>
                        <YAxis interval={0} tickFormatter={!showDetails && formatNumber} />
                        <Tooltip 
                            formatter={(value, name, props) => {
                                if(name === 'totalQuantity') return !showDetails ? formatNumber(value) : value
                                if(name === 'profitPercent') return `${(data.profitPercent * 100).toFixed(2)}%`
                                return value
                            }}
                        />
                        <Bar dataKey="totalQuantity">
                            {data.map((entry, index) => (
                                <Cell key={`cell-${index}`} fill={getMarginColor(entry.totalQuantity)} />
                            ))}
                            <LabelList 
                                dataKey="profitPercent" 
                                position="top" 
                                offset={3} 
                                formatter={(value, name, entry) => { if(!entry) return value+"%" }} 
                                style={{ fontSize: 10, fill: '#00C49F' }} />
                            <LabelList 
                                dataKey="totalQuantity" 
                                position="bottom" 
                                offset={-10} 
                                formatter={!showDetails ? (value) => formatNumber(value) : undefined} 
                                style={{ fontSize: 10, fill: '#3a3a3aff', fontWeight: 'bold' }}
                            />
                        </Bar>
                    </BarChart>
                </ResponsiveContainer>
            </CardContent>
        </Card>
    )
}

export default TopProducts
