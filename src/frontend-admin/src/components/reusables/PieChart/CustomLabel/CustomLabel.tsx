import React from 'react'
import {IRechartPieDatum} from '../PieChart'

interface ICustomLabelProps {
    cx?: number
    cy?: number
    midAngle?: number
    innerRadius?: number
    outerRadius?: number
    index?: number
    data: IRechartPieDatum[]
}
export const CustomLabel: React.FunctionComponent<ICustomLabelProps> = props => {
    const {cx = 300, cy = 300, midAngle = 5, innerRadius = 5, outerRadius = 5, index = 5, data} = props
    const radian = Math.PI / 180

    var totalValue = 0

    //compares to the percent of the pie and pops the text
    //out of the pie if it too small

    for (let i in data) {
        //get the total value
        totalValue = totalValue + data[i].value
    }
    const sizeOfSlice = data[index].value / totalValue

    var radius
    var textColor
    if (data[index].value === 0) {
        radius = innerRadius + (outerRadius - innerRadius) * 1.31
        textColor = '#f4f4f400'
    } else if (sizeOfSlice < 0.15) {
        radius = innerRadius + (outerRadius - innerRadius) * 1.15
        textColor = '#000'
    } else {
        radius = innerRadius + (outerRadius - innerRadius) * 0.5
        textColor = 'white'
    }

    const x = cx + radius * Math.cos(-midAngle * radian)
    const y = cy + radius * Math.sin(-midAngle * radian)

    return (
        <text
            x={x}
            y={y}
            fontSize='14'
            fill={textColor}
            fontWeight='bold'
            textAnchor='middle'
            dominantBaseline='middle'
        >
            ${Number(data[index].value.toFixed(2))}
        </text>
    )
}
